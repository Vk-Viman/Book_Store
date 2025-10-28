using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
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

        var userId = int.Parse(User.FindFirst("sub")!.Value);

        // verify product exists and has enough stock (if applicable)
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound(new { message = "Product not found." });

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

        if (item == null)
        {
            // clamp quantity to product stock if stock is positive
            var qty = quantity;
            if (product.StockQty > 0 && qty > product.StockQty) qty = product.StockQty;
            item = new CartItem { UserId = userId, ProductId = productId, Quantity = qty };
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

    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item == null) return NotFound();
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
