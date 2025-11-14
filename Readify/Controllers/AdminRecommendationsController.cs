using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Readify.Data;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/recommendations")]
[Authorize(Roles = "Admin")]
public class AdminRecommendationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public AdminRecommendationsController(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    // POST api/admin/recommendations/refresh -> clears all recommendation caches
    [HttpPost("refresh")]
    public IActionResult RefreshAll()
    {
        // No built-in way to enumerate IMemoryCache entries; use a convention: store known keys or clear entire cache
        try
        {
            // Attempt to clear cache by creating a new instance is not possible here; instead, use a backing collection pattern.
            // As a pragmatic approach, we will clear entries for users who have wishlists by enumerating them and removing keys.
            var users = _db.Wishlists.Select(w => w.UserId).Distinct().ToList();
            foreach (var u in users)
            {
                var key = $"recommendations:user:{u}";
                _cache.Remove(key);
            }
            return Ok(new { message = "Recommendation caches cleared for users with wishlists.", count = users.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to clear recommendation cache", error = ex.Message });
        }
    }
}
