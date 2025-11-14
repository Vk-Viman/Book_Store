using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminReviewsController> _logger;
    private readonly IMemoryCache _cache;

    public AdminReviewsController(AppDbContext db, ILogger<AdminReviewsController> logger, IMemoryCache cache)
    {
        _db = db; _logger = logger; _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? q = null)
    {
        var query = _db.Reviews.Where(r => !r.IsApproved);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tq = q.Trim();
            query = query.Where(r => r.Comment.Contains(tq));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] bool approve)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r == null) return NotFound();
        r.IsApproved = approve;
        await _db.SaveChangesAsync();

        try
        {
            // recalc avg rating for the product
            var avg = await _db.Reviews.Where(x => x.ProductId == r.ProductId && x.IsApproved).AverageAsync(x => (decimal?)x.Rating);
            var prod = await _db.Products.FindAsync(r.ProductId);
            if (prod != null)
            {
                prod.AvgRating = avg == null ? null : Math.Round(avg.Value, 2);
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to recalc avg rating for product {ProductId}", r.ProductId);
        }

        // invalidate recommendations cache for users affected by this product's rating change
        try
        {
            var affected = await _db.Wishlists.Where(w => w.ProductId == r.ProductId).Select(w => w.UserId).Distinct().ToListAsync();
            foreach (var u in affected)
            {
                var key = $"recommendations:user:{u}";
                _cache.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate recommendations cache after review approval for product {ProductId}", r.ProductId);
        }

        return Ok(r);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkApproveRequest req)
    {
        if (req == null || req.Ids == null || req.Ids.Count == 0) return BadRequest(new { message = "No ids provided" });

        // load reviews
        var reviews = await _db.Reviews.Where(r => req.Ids.Contains(r.Id)).ToListAsync();
        if (reviews.Count == 0) return NotFound(new { message = "No matching reviews found" });

        // set approval flag
        foreach (var r in reviews) r.IsApproved = req.Approve;
        await _db.SaveChangesAsync();

        try
        {
            // recalc avg per affected product
            var productIds = reviews.Select(r => r.ProductId).Distinct().ToList();
            foreach (var pid in productIds)
            {
                var avg = await _db.Reviews.Where(x => x.ProductId == pid && x.IsApproved).AverageAsync(x => (decimal?)x.Rating);
                var prod = await _db.Products.FindAsync(pid);
                if (prod != null)
                {
                    prod.AvgRating = avg == null ? null : Math.Round(avg.Value, 2);
                }
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to recalc avg rating after bulk update");
        }

        // invalidate recommendation cache for users who have wishlisted any affected product
        try
        {
            var affectedProductIds = reviews.Select(r => r.ProductId).Distinct().ToList();
            var affectedUsers = await _db.Wishlists.Where(w => affectedProductIds.Contains(w.ProductId)).Select(w => w.UserId).Distinct().ToListAsync();
            foreach (var u in affectedUsers)
            {
                var key = $"recommendations:user:{u}";
                _cache.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate recommendations cache after bulk review update");
        }

        return Ok(new { updated = reviews.Count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r == null) return NotFound();

        var productId = r.ProductId;
        _db.Reviews.Remove(r);
        await _db.SaveChangesAsync();

        // recalc avg for product after deletion
        try
        {
            var avg = await _db.Reviews.Where(x => x.ProductId == productId && x.IsApproved).AverageAsync(x => (decimal?)x.Rating);
            var prod = await _db.Products.FindAsync(productId);
            if (prod != null)
            {
                prod.AvgRating = avg == null ? null : Math.Round(avg.Value, 2);
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to recalc avg rating after deleting review {ReviewId}", id);
        }

        // invalidate recommendation cache for affected users
        try
        {
            var affected = await _db.Wishlists.Where(w => w.ProductId == productId).Select(w => w.UserId).Distinct().ToListAsync();
            foreach (var u in affected)
            {
                var key = $"recommendations:user:{u}";
                _cache.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate recommendations cache after deleting review {ReviewId}", id);
        }

        return NoContent();
    }

    public class BulkApproveRequest
    {
        public List<int> Ids { get; set; } = new List<int>();
        public bool Approve { get; set; }
    }
}
