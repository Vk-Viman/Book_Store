using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace Readify.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CartController> _logger;

    public CartController(AppDbContext context, ILogger<CartController> logger) => (_context, _logger) = (context, logger);

    private async Task<int?> GetUserIdFromClaimsAsync()
    {
        // Prefer explicit "userId" claim set by JwtHelper.
        var uid = User.FindFirst("userId")?.Value;
        if (!string.IsNullOrEmpty(uid) && int.TryParse(uid, out var parsed)) return parsed;

        // Try NameIdentifier
        uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(uid) && int.TryParse(uid, out parsed)) return parsed;

        // Fallback: Jwt 'sub' may contain email. If it's an int-like string, parse; otherwise treat as email lookup.
        uid = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(uid))
        {
            if (int.TryParse(uid, out parsed)) return parsed;
            // treat as email and lookup user
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == uid);
                if (user != null) return user.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to lookup user by email from sub claim.");
            }
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized();

            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCart failed");
            return StatusCode(500, new { message = "Failed to retrieve cart" });
        }
    }

    // Admin: get cart for any user
    [Authorize(Roles = "Admin")]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetCartForUser(int userId)
    {
        var items = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("{productId}")]
    public async Task<IActionResult> AddToCart(int productId, [FromQuery] int quantity = 1)
    {
        if (quantity <= 0) return BadRequest(new { message = "Quantity must be at least 1." });

        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized();

            // verify product exists and has enough stock (if applicable)
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound(new { message = "Product not found." });

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);

            if (item == null)
            {
                // clamp quantity to product stock if stock is positive
                var qty = quantity;
                if (product.StockQty > 0 && qty > product.StockQty) qty = product.StockQty;
                item = new CartItem { UserId = userId.Value, ProductId = productId, Quantity = qty };
                _context.CartItems.Add(item);
            }
            else
            {
                var newQty = item.Quantity + quantity;
                if (product.StockQty > 0 && newQty > product.StockQty) newQty = product.StockQty;
                item.Quantity = newQty;
            }
            await _context.SaveChangesAsync();
            // reload with product navigation
            await _context.Entry(item).Reference(i => i.Product).LoadAsync();
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddToCart failed");
            return StatusCode(500, new { message = "Failed to add to cart" });
        }
    }

    // Update quantity for an item by productId
    [HttpPut("update")]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartRequest req)
    {
        if (req.Quantity < 0) return BadRequest(new { message = "Quantity cannot be negative" });
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized();

            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == req.ProductId);
            if (item == null) return NotFound(new { message = "Cart item not found" });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == req.ProductId);
            if (product == null) return NotFound(new { message = "Product not found" });

            if (req.Quantity == 0)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return NoContent();
            }

            if (product.StockQty > 0 && req.Quantity > product.StockQty) return BadRequest(new { message = "Requested quantity exceeds stock" });

            item.Quantity = req.Quantity;
            await _context.SaveChangesAsync();
            await _context.Entry(item).Reference(i => i.Product).LoadAsync();
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateQuantity failed");
            return StatusCode(500, new { message = "Failed to update cart item" });
        }
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);
            if (item == null) return NotFound();
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveFromCart failed");
            return StatusCode(500, new { message = "Failed to remove from cart" });
        }
    }
}

public class UpdateCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
