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
            if (userId == null) return Unauthorized(new { message = "User not authenticated" });

            var cartItems = await _context.CartItems.Include(c => c.Product)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            _logger.LogInformation("Checkout requested by user {UserId} with {Count} cart items", userId.Value, cartItems.Count);

            if (!cartItems.Any()) return BadRequest(new { message = "Cart is empty." });

            var computedTotal = cartItems.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
            _logger.LogInformation("Computed cart total for user {UserId} is {Total}", userId.Value, computedTotal);

            // Use a transaction to ensure order creation and cart removal are atomic
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate and update stock
                foreach (var c in cartItems)
                {
                    var p = c.Product; // included
                    if (p == null)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { message = $"Product {c.ProductId} not found" });
                    }

                    // if stock tracking enabled (StockQty >= 0 assumed), check
                    if (p.StockQty > 0)
                    {
                        if (p.StockQty < c.Quantity)
                        {
                            await tx.RollbackAsync();
                            return BadRequest(new { message = $"Insufficient stock for product '{p.Title}'. Available: {p.StockQty}, requested: {c.Quantity}" });
                        }
                        p.StockQty -= c.Quantity;
                        // ensure EF marks the property as modified
                        _context.Entry(p).Property(pp => pp.StockQty).IsModified = true;
                    }
                }

                var order = new Order
                {
                    UserId = userId.Value,
                    Items = cartItems.Select(c => new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Product?.Price ?? 0m
                    }).ToList(),
                    TotalAmount = computedTotal
                };

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                // After save, explicitly log updated stock values for debugging
                foreach (var c in cartItems)
                {
                    try
                    {
                        var refreshed = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == c.ProductId);
                        _logger.LogInformation("Post-checkout stock for product {ProductId} ('{Title}') is {StockQty}", c.ProductId, refreshed?.Title, refreshed?.StockQty);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read product {ProductId} after checkout", c.ProductId);
                    }
                }

                await tx.CommitAsync();

                _logger.LogInformation("Order {OrderId} created for user {UserId}", order.Id, userId.Value);

                // reload saved order including items and product details
                var savedOrder = await _context.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

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

                return Ok(savedOrder);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error during checkout");
                await tx.RollbackAsync();
#if DEBUG
                return StatusCode(500, new { message = "Failed to checkout - database error", error = dbEx.ToString() });
#else
                return StatusCode(500, new { message = "Failed to checkout" });
#endif
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout failed");
#if DEBUG
            return StatusCode(500, new { message = "Failed to checkout", error = ex.ToString() });
#else
            return StatusCode(500, new { message = "Failed to checkout" });
#endif
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized(new { message = "User not authenticated" });

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
