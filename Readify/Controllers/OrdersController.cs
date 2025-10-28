using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.Services;

namespace Readify.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _email;

    public OrdersController(AppDbContext context, IEmailService email) => (_context, _email) = (context, email);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        var cartItems = await _context.CartItems.Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any()) return BadRequest("Cart is empty.");

        var order = new Order
        {
            UserId = userId,
            Items = cartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                UnitPrice = c.Product?.Price ?? 0m
            }).ToList(),
            TotalAmount = cartItems.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity)
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        // send confirmation via logging email (non-blocking)
        try
        {
            _ = _email.SendTemplateAsync((await _context.Users.FindAsync(userId))?.Email ?? string.Empty, "OrderConfirmation", new { OrderId = order.Id });
        }
        catch
        {
            // ignore
        }

        return Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        var orders = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .ToListAsync();
        return Ok(orders);
    }
}
