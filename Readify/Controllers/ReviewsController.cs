using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(AppDbContext db, ILogger<ReviewsController> logger)
    {
        _db = db; _logger = logger;
    }

    private int? CurrentUserId()
    {
        var uid = User.FindFirst("userId")?.Value;
        if (int.TryParse(uid, out var id)) return id;
        return null;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] Review rv)
    {
        var uid = CurrentUserId();
        if (!uid.HasValue) return Unauthorized();
        rv.UserId = uid.Value;
        rv.CreatedAt = DateTime.UtcNow;
        rv.IsApproved = false; // require moderation
        _db.Reviews.Add(rv);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetByProduct), new { productId = rv.ProductId }, rv);
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var reviews = await _db.Reviews.Where(r => r.ProductId == productId && r.IsApproved).OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(reviews);
    }
}
