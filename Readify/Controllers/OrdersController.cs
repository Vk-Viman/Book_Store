using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace Readify.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _email;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(AppDbContext context, IEmailService email, ILogger<OrdersController> logger) => (_context, _email, _logger) = (context, email, logger);

    private async Task<int?> GetUserIdFromClaimsAsync()
    {
        var uid = User.FindFirst("userId")?.Value;
        if (!string.IsNullOrEmpty(uid) && int.TryParse(uid, out var parsed)) return parsed;

        uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(uid) && int.TryParse(uid, out parsed)) return parsed;

        uid = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(uid))
        {
            if (int.TryParse(uid, out parsed)) return parsed;
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

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized();

            var cartItems = await _context.CartItems.Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            if (!cartItems.Any()) return BadRequest("Cart is empty.");

            var order = new Order
            {
                UserId = userId.Value,
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
                var user = await _context.Users.FindAsync(userId.Value);
                var emailTo = user?.Email ?? string.Empty;
                _ = _email.SendTemplateAsync(emailTo, "OrderConfirmation", new { OrderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order confirmation email for order {OrderId}", order.Id);
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout failed");
            return StatusCode(500, new { message = "Failed to checkout" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized();

            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId.Value)
                .ToListAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrders failed");
            return StatusCode(500, new { message = "Failed to load orders" });
        }
    }
}
