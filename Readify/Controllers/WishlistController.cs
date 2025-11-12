using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public WishlistController(AppDbContext db, ILogger<WishlistController> logger)
    {
        _db = db;
        _logger = logger;
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
        var items = await _db.Wishlists.Include(w => w.Product).Where(w => w.UserId == uid.Value).Select(w => new { w.ProductId, Product = new { w.Product.Id, w.Product.Title, w.Product.ImageUrl, w.Product.Price } }).ToListAsync();
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
        return NoContent();
    }
}
