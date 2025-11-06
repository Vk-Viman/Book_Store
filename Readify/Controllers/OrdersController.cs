using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using Readify.DTOs;
using System.Threading;

namespace Readify.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _email;
    private readonly ILogger<OrdersController> _logger;
    private readonly IShippingService _shipping;

    public OrdersController(AppDbContext context, IEmailService email, ILogger<OrdersController> logger, IShippingService shipping) => (_context, _email, _logger, _shipping) = (context, email, logger, shipping);

    private async Task<int?> GetUserIdFromClaimsAsync()
    {
        string? uid = User.FindFirst("userId")?.Value;
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

    private static DateTime ConvertUtcToSriLanka(DateTime utc)
    {
        try
        {
            // ensure utc kind
            var utcTime = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            // Try Windows id first, fall back to IANA
            TimeZoneInfo? tz = null;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"); } catch { }
            if (tz == null)
            {
                try { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); } catch { }
            }
            if (tz == null)
            {
                // fallback to adding offset of +5:30
                return utcTime.AddHours(5).AddMinutes(30);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
        catch
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
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

            var subtotal = cartItems.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);

            _logger.LogInformation("Computed cart subtotal for user {UserId} is {Subtotal}", userId.Value, subtotal);

            // Apply promo code if provided
            decimal? discountPercent = null;
            decimal discountAmount = 0m;
            bool freeShipping = false;
            string? appliedType = null;

            if (!string.IsNullOrWhiteSpace(dto?.PromoCode))
            {
                try
                {
                    var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code == dto.PromoCode && p.IsActive);
                    if (promo != null)
                    {
                        appliedType = promo.Type;
                        if (string.Equals(promo.Type, "Percentage", StringComparison.OrdinalIgnoreCase))
                        {
                            discountPercent = promo.DiscountPercent;
                            discountAmount = Math.Round((subtotal * (decimal)promo.DiscountPercent) / 100m, 2);
                        }
                        else if (string.Equals(promo.Type, "Fixed", StringComparison.OrdinalIgnoreCase))
                        {
                            discountAmount = promo.FixedAmount ?? 0m;
                            discountPercent = null;
                        }
                        else if (string.Equals(promo.Type, "FreeShipping", StringComparison.OrdinalIgnoreCase))
                        {
                            freeShipping = true;
                        }

                        _logger.LogInformation("Applied promo code {Code} type {Type} discount amount {Amount}", promo.Code, promo.Type, discountAmount);
                    }
                    else
                    {
                        _logger.LogInformation("Promo code {Code} not found or inactive", dto.PromoCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate promo code {Code}", dto.PromoCode);
                }
            }

            // server calculates shipping using shipping service; do not trust client-provided shippingCost
            var region = dto?.Region ?? "national";
            var shippingCost = await _shipping.GetRateAsync(region, subtotal);
            if (freeShipping) shippingCost = 0m;

            // compute total = subtotal + shipping - discount
            var computedTotal = subtotal + shippingCost - discountAmount;
            if (computedTotal < 0) computedTotal = 0m;

            // Use a transaction to ensure order creation and cart removal are atomic
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate and update stock using a single atomic SQL UPDATE per product
                foreach (var c in cartItems)
                {
                    // Ensure product exists
                    if (c.Product == null)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { message = $"Product {c.ProductId} not found" });
                    }

                    // Perform atomic decrement only if sufficient stock exists
                    var rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"UPDATE Product SET StockQty = StockQty - {c.Quantity} WHERE Id = {c.ProductId} AND StockQty >= {c.Quantity}");
                    if (rows == 0)
                    {
                        // No rows updated => insufficient stock or product missing
                        await tx.RollbackAsync();
                        return BadRequest(new { message = $"Insufficient stock for product '{c.Product.Title}'. Available: {c.Product.StockQty}, requested: {c.Quantity}" });
                    }
                }

                // If a payment service is registered in DI, use it
                bool paymentSucceeded = false;
                string? paymentTxId = null;
                try
                {
                    var payment = HttpContext.RequestServices.GetService(typeof(IPaymentService)) as IPaymentService;
                    if (payment != null)
                    {
                        // allow client to send a payment token in header for testing flaky/fail behaviour
                        var payToken = Request.Headers["X-Payment-Token"].FirstOrDefault();
                        var (success, transactionId) = await payment.ChargeAsync(computedTotal, token: payToken);
                        if (!success)
                        {
                            await tx.RollbackAsync();
                            return BadRequest(new { message = "Payment declined" });
                        }
                        paymentSucceeded = true;
                        paymentTxId = transactionId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Payment processing failed (mock)");
                }

                var order = new Order
                {
                    UserId = userId.Value,
                    OrderDate = DateTime.UtcNow, // ensure stored time is UTC
                    Items = cartItems.Select(c => new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Product?.Price ?? 0m
                    }).ToList(),
                    TotalAmount = computedTotal,
                    ShippingName = dto?.ShippingName,
                    ShippingAddress = dto?.ShippingAddress,
                    ShippingPhone = dto?.ShippingPhone,
                    ShippingCost = shippingCost,
                    PromoCode = string.IsNullOrWhiteSpace(dto?.PromoCode) ? null : dto.PromoCode,
                    DiscountPercent = discountPercent,
                    DiscountAmount = discountAmount,
                    FreeShipping = freeShipping,
                    // set payment/order status fields
                    PaymentStatus = paymentSucceeded ? "Paid" : "Pending",
                    OrderStatus = "Processing",
                    PaymentTransactionId = paymentTxId
                };

                try
                {
                    // Retry loop for DbUpdateConcurrencyException
                    var attempts = 0;
                    const int maxAttempts = 3;
                    while (true)
                    {
                        try
                        {
                            _context.Orders.Add(order);
                            _context.CartItems.RemoveRange(cartItems);
                            await _context.SaveChangesAsync();
                            break; // success
                        }
                        catch (DbUpdateConcurrencyException concEx)
                        {
                            attempts++;
                            _logger.LogWarning(concEx, "Concurrency conflict during checkout attempt {Attempt}", attempts);
                            if (attempts >= maxAttempts)
                            {
                                throw;
                            }
                            // Backoff before retrying
                            await Task.Delay(200 * attempts);
                            // refresh tracked product entities' RowVersion by reloading from db
                            foreach (var c in cartItems)
                            {
                                var fresh = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == c.ProductId);
                                if (fresh != null)
                                {
                                    // Update the local included product reference if any
                                    c.Product = fresh;
                                }
                            }
                        }
                    }

                    // After save, explicitly read updated stock values for logging
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

                    // Do not convert OrderDate to a specific timezone on the server. Return UTC and let the client format for the user's locale.

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

    // POST api/orders/create -> alias to checkout for Phase 5
    [HttpPost("create")]
    public Task<IActionResult> Create([FromBody] CheckoutDto dto) => Checkout(dto);

    // GET api/orders/{id} - return order details for owner or admin
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized(new { message = "User not authenticated" });

            var order = await _context.Orders.Include(o => o.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { message = "Order not found" });

            // allow admin to view any order
            if (order.UserId != userId.Value && !User.IsInRole("Admin")) return Forbid();

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrder failed");
            return StatusCode(500, new { message = "Failed to load order" });
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

            // Return OrderDate in UTC; clients should convert to local display time.

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrders failed");
            return StatusCode(500, new { message = "Failed to load orders" });
        }
    }

    // DELETE api/orders/{id} - allow user to cancel their own order if still Processing
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        try
        {
            var userId = await GetUserIdFromClaimsAsync();
            if (userId == null) return Unauthorized(new { message = "User not authenticated" });

            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { message = "Order not found" });

            // allow admins to delete via admin controller; here only owners can cancel
            if (order.UserId != userId.Value && !User.IsInRole("Admin")) return Forbid();

            if (order.OrderStatus != "Processing")
            {
                return BadRequest(new { message = "Only orders in 'Processing' state can be cancelled." });
            }

            // mark as cancelled
            order.OrderStatus = "Cancelled";

            // If payment was taken, try to refund (best-effort)
            if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var payment = HttpContext.RequestServices.GetService(typeof(IPaymentService)) as IPaymentService;
                    if (payment != null)
                    {
                        var refunded = await payment.RefundAsync(order.TotalAmount, order.PaymentTransactionId);
                        order.PaymentStatus = refunded ? "Refunded" : "Refund Failed";
                    }
                    else
                    {
                        order.PaymentStatus = "Paid"; // leave as Paid if no payment service available
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Refund failed for order {OrderId}", id);
                    order.PaymentStatus = "Refund Failed";
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId.Value);

            // send optional email
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                var emailTo = user?.Email ?? string.Empty;
                _ = _email.SendTemplateAsync(emailTo, "OrderCancelled", new { OrderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation email for order {OrderId}", order.Id);
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelOrder failed");
            return StatusCode(500, new { message = "Failed to cancel order" });
        }
    }
}
