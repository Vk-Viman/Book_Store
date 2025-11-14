using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<WishlistController> _logger;
    private readonly IMemoryCache _cache;

    public WishlistController(AppDbContext db, ILogger<WishlistController> logger, IMemoryCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    private int? CurrentUserId()
    {
        var uid = User.FindFirst("userId")?.Value;
        if (int.TryParse(uid, out var id)) return id;
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWishlist()
    {
        var uid = CurrentUserId();
        if (!uid.HasValue) return Unauthorized();

        // materialize entries and project safely to avoid nullable dereference warnings
        var raw = await _db.Wishlists.Include(w => w.Product).Where(w => w.UserId == uid.Value).ToListAsync();
        var items = raw.Select(w => new
        {
            w.ProductId,
            Product = w.Product == null ? null : new { w.Product.Id, w.Product.Title, w.Product.ImageUrl, w.Product.Price, w.Product.AvgRating }
        }).ToList();

        return Ok(items);
    }

    [HttpPost("{productId}")]
    public async Task<IActionResult> Add(int productId)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue) return Unauthorized();
        var exists = await _db.Wishlists.FindAsync(uid.Value, productId);
        if (exists != null) return BadRequest(new { message = "Already in wishlist" });
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound(new { message = "Product not found" });
        var w = new Wishlist { UserId = uid.Value, ProductId = productId, CreatedAt = DateTime.UtcNow };
        _db.Wishlists.Add(w);
        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} added product {ProductId} to wishlist", uid.Value, productId);

        // invalidate recommendations cache for affected users (current user + users who have this product in wishlist)
        try
        {
            var affected = await _db.Wishlists.Where(x => x.ProductId == productId).Select(x => x.UserId).Distinct().ToListAsync();
            foreach (var u in affected)
            {
                var key = $"recommendations:user:{u}";
                _cache.Remove(key);
            }
            // also remove current user's cache key (if not included)
            var meKey = $"recommendations:user:{uid.Value}";
            _cache.Remove(meKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate recommendations cache after wishlist add");
        }

        return CreatedAtAction(nameof(GetMyWishlist), null);
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue) return Unauthorized();
        var w = await _db.Wishlists.FindAsync(uid.Value, productId);
        if (w == null) return NotFound();
        _db.Wishlists.Remove(w);
        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} removed product {ProductId} from wishlist", uid.Value, productId);

        // invalidate recommendations cache similarly
        try
        {
            var affected = await _db.Wishlists.Where(x => x.ProductId == productId).Select(x => x.UserId).Distinct().ToListAsync();
            foreach (var u in affected)
            {
                var key = $"recommendations:user:{u}";
                _cache.Remove(key);
            }
            var meKey = $"recommendations:user:{uid.Value}";
            _cache.Remove(meKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate recommendations cache after wishlist remove");
        }

        return NoContent();
    }
}
