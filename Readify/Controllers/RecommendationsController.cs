using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Readify.Data;

namespace Readify.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public RecommendationsController(AppDbContext db, IMemoryCache cache)
    {
        _db = db; _cache = cache;
    }

    private int? CurrentUserId()
    {
        var uid = User.FindFirst("userId")?.Value;
        if (int.TryParse(uid, out var id)) return id;
        return null;
    }

    // GET api/recommendations/me
    [HttpGet("me")]
    public async Task<IActionResult> GetForMe()
    {
        var uid = CurrentUserId();
        if (!uid.HasValue) return Unauthorized();

        var cacheKey = $"recommendations:user:{uid.Value}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return Ok(cached);
        }

        try
        {
            // load user's wishlist
            var myWishlist = await _db.Wishlists.Where(w => w.UserId == uid.Value).Select(w => w.ProductId).ToListAsync();

            // If user has no wishlist, return popular items instead of empty
            if (!myWishlist.Any())
            {
                var popular = await GetPopularItemsAsync(myWishlist);
                _cache.Set(cacheKey, popular, TimeSpan.FromMinutes(30));
                return Ok(popular);
            }

            // find other users who have overlapping wishlist items
            var others = await _db.Wishlists.Where(w => myWishlist.Contains(w.ProductId) && w.UserId != uid.Value)
                .Select(w => new { w.UserId, w.ProductId }).ToListAsync();

            // count co-occurrence by product for candidates not in my wishlist
            var coCounts = others.GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .Where(x => !myWishlist.Contains(x.ProductId))
                .ToList();

            // if no co-occurrence candidates, return popular items instead
            if (!coCounts.Any())
            {
                var popular = await GetPopularItemsAsync(myWishlist);
                _cache.Set(cacheKey, popular, TimeSpan.FromMinutes(30));
                return Ok(popular);
            }

            // also get global popularity for weighting (total wishlist count per product)
            var popularity = await _db.Wishlists.GroupBy(w => w.ProductId).Select(g => new { ProductId = g.Key, Total = g.Count() }).ToListAsync();
            var popLookup = popularity.ToDictionary(p => p.ProductId, p => p.Total);

            // build score = co-occurrence count * sqrt(popularity) * (1 + normalized avgRating)
            var productIds = coCounts.Select(c => c.ProductId).ToList();
            var prods = await _db.Products.Where(p => productIds.Contains(p.Id)).Select(p => new { p.Id, p.Title, p.ImageUrl, p.Price, p.AvgRating }).ToListAsync();

            var scored = coCounts.Select(c =>
            {
                var pop = popLookup.ContainsKey(c.ProductId) ? popLookup[c.ProductId] : 0;
                var prod = prods.FirstOrDefault(p => p.Id == c.ProductId);
                var rating = prod?.AvgRating ?? 0m;
                // numeric score: use sqrt to soften heavy popularity influence
                var score = c.Count * Math.Sqrt(1 + pop);
                // normalize rating to 0..1 (rating 0..5)
                score = score * (1.0 + (double)rating / 5.0);
                return new { ProductId = c.ProductId, Score = score };
            }).OrderByDescending(x => x.Score).Take(20).ToList();

            var resultProducts = prods.Where(p => scored.Select(s => s.ProductId).Contains(p.Id))
                .OrderByDescending(p => scored.First(s => s.ProductId == p.Id).Score)
                .Select(p => new { p.Id, p.Title, p.ImageUrl, p.Price, p.AvgRating })
                .ToList();

            var payload = new { items = resultProducts };
            _cache.Set(cacheKey, payload, TimeSpan.FromMinutes(30));
            return Ok(payload);
        }
        catch (Exception ex)
        {
            // log and return popular items so UI degrades gracefully
            try { var logger = HttpContext?.RequestServices.GetService(typeof(ILogger<RecommendationsController>)) as ILogger<RecommendationsController>; logger?.LogWarning(ex, "Recommendations failed"); } catch { }
            var popular = await GetPopularItemsAsync(await _db.Wishlists.Where(w => w.UserId == uid.Value).Select(w => w.ProductId).ToListAsync());
            return Ok(popular);
        }
    }

    private async Task<object> GetPopularItemsAsync(List<int> exclude)
    {
        var pop = await _db.Wishlists.GroupBy(w => w.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(20)
            .ToListAsync();

        var popIds = pop.Select(p => p.ProductId).Where(id => !exclude.Contains(id)).ToList();
        if (!popIds.Any()) return new { items = Array.Empty<object>() };

        var prods = await _db.Products.Where(p => popIds.Contains(p.Id)).Select(p => new { p.Id, p.Title, p.ImageUrl, p.Price, p.AvgRating }).ToListAsync();
        var ordered = popIds.Select(id => prods.FirstOrDefault(p => p.Id == id)).Where(p => p != null).Cast<object>().ToList();
        return new { items = ordered };
    }

    // POST api/recommendations/refresh -> invalidate and recompute for current user
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshForMe()
    {
        var uid = CurrentUserId();
        if (!uid.HasValue) return Unauthorized();
        var key = $"recommendations:user:{uid.Value}";
        _cache.Remove(key);
        // trigger a fresh computation by calling the same method
        return await GetForMe();
    }

    // Public recommendations endpoint for anonymous users or to guarantee a result
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic()
    {
        try
        {
            var cacheKey = "recommendations:public";
            if (_cache.TryGetValue(cacheKey, out object? cached)) return Ok(cached);

            // top products by wishlist count
            var pop = await _db.Wishlists.GroupBy(w => w.ProductId)
                .Select(g => new { ProductId = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(20)
                .ToListAsync();

            var ids = pop.Select(p => p.ProductId).ToList();
            if (!ids.Any()) return Ok(new { items = Array.Empty<object>() });

            var prods = await _db.Products.Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Title, p.ImageUrl, p.Price, p.AvgRating }).ToListAsync();
            var ordered = ids.Select(id => prods.FirstOrDefault(p => p.Id == id)).Where(p => p != null).Cast<object>().ToList();
            var payload = new { items = ordered };
            _cache.Set(cacheKey, payload, TimeSpan.FromMinutes(30));
            return Ok(payload);
        }
        catch (Exception ex)
        {
            try { var logger = HttpContext?.RequestServices.GetService(typeof(ILogger<RecommendationsController>)) as ILogger<RecommendationsController>; logger?.LogWarning(ex, "Public recommendations failed"); } catch { }
            return Ok(new { items = Array.Empty<object>() });
        }
    }
}
