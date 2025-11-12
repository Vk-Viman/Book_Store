using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.Services;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminOrdersController> _logger;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public AdminOrdersController(AppDbContext context, ILogger<AdminOrdersController> logger, IEmailService email, IAuditService audit)
    {
        _context = context;
        _logger = logger;
        _email = email;
        _audit = audit;
    }

    // GET api/admin/orders
    // supports server-side paging, optional status filter, search (q), and sorting
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? status = null, [FromQuery] string? q = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null)
    {
        try
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            var query = _context.Orders.Include(o => o.Items).ThenInclude(i => i.Product).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                query = query.Where(o => o.OrderStatusString == s || o.PaymentStatus == s || o.Status == s);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var tq = q.Trim();
                // if numeric, search by Id
                if (int.TryParse(tq, out var id))
                {
                    query = query.Where(o => o.Id == id);
                }
                else
                {
                    query = query.Where(o => (o.PaymentTransactionId ?? string.Empty).Contains(tq) || (o.PromoCode ?? string.Empty).Contains(tq) || (o.OrderStatusString ?? string.Empty).Contains(tq) || (o.PaymentStatus ?? string.Empty).Contains(tq));
                }
            }

            // Sorting
            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.ToLowerInvariant())
                {
                    case "date": query = desc ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate); break;
                    case "total": query = desc ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount); break;
                    case "status": query = desc ? query.OrderByDescending(o => o.OrderStatusString) : query.OrderBy(o => o.OrderStatusString); break;
                    default: query = query.OrderByDescending(o => o.OrderDate); break;
                }
            }
            else
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // map to payload with orderStatus property for frontend compatibility
            var payload = items.Select(o => new
            {
                o.Id,
                o.UserId,
                OrderDate = o.OrderDate,
                o.TotalAmount,
                orderStatus = o.OrderStatusString,
                paymentStatus = o.PaymentStatus,
                paymentTransactionId = o.PaymentTransactionId,
                promoCode = o.PromoCode,
                updatedAt = o.UpdatedAt,
                dateDelivered = o.DateDelivered,
                items = o.Items.Select(i => new { i.Id, i.ProductId, i.Quantity, i.UnitPrice, product = new { i.Product?.Id, i.Product?.Title, i.Product?.ImageUrl } })
            }).ToList();

            return Ok(new { items = payload, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list orders");
            return StatusCode(500, new { message = "Failed to list orders" });
        }
    }

    // GET api/admin/orders/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var order = await _context.Orders.Include(o => o.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { message = "Order not found" });

            var payload = new
            {
                order.Id,
                order.UserId,
                OrderDate = order.OrderDate,
                order.TotalAmount,
                orderStatus = order.OrderStatusString,
                paymentStatus = order.PaymentStatus,
                paymentTransactionId = order.PaymentTransactionId,
                promoCode = order.PromoCode,
                updatedAt = order.UpdatedAt,
                dateDelivered = order.DateDelivered,
                items = order.Items.Select(i => new { i.Id, i.ProductId, i.Quantity, i.UnitPrice, product = new { i.Product?.Id, i.Product?.Title, i.Product?.ImageUrl } })
            };

            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order {OrderId}", id);
            return StatusCode(500, new { message = "Failed to get order" });
        }
    }

    // GET api/admin/orders/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        try
        {
            var orders = await _context.Orders.Include(o => o.Items).ThenInclude(i => i.Product).Where(o => o.UserId == userId).ToListAsync();
            var payload = orders.Select(order => new
            {
                order.Id,
                order.UserId,
                OrderDate = order.OrderDate,
                order.TotalAmount,
                orderStatus = order.OrderStatusString,
                paymentStatus = order.PaymentStatus,
                paymentTransactionId = order.PaymentTransactionId,
                promoCode = order.PromoCode,
                updatedAt = order.UpdatedAt,
                dateDelivered = order.DateDelivered,
                items = order.Items.Select(i => new { i.Id, i.ProductId, i.Quantity, i.UnitPrice, product = new { i.Product?.Id, i.Product?.Title, i.Product?.ImageUrl } })
            }).ToList();
            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to get user orders" });
        }
    }

    public class UpdateStatusDto { public string? OrderStatus { get; set; } public string? PaymentStatus { get; set; } }

    // PUT api/admin/orders/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { message = "Order not found" });

            var changed = false;
            var prevOrderStatus = order.OrderStatusString;
            if (!string.IsNullOrWhiteSpace(dto.OrderStatus) && !string.Equals(order.OrderStatusString, dto.OrderStatus, StringComparison.OrdinalIgnoreCase))
            {
                // validate incoming status is known
                if (!Enum.TryParse<OrderStatus>(dto.OrderStatus, true, out var parsed))
                {
                    return BadRequest(new { message = "Invalid order status" });
                }
                order.OrderStatus = parsed;
                order.UpdatedAt = DateTime.UtcNow; // track update time
                if (parsed == OrderStatus.Delivered)
                {
                    order.DateDelivered = DateTime.UtcNow;
                }
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.PaymentStatus) && !string.Equals(order.PaymentStatus, dto.PaymentStatus, StringComparison.OrdinalIgnoreCase))
            {
                order.PaymentStatus = dto.PaymentStatus!.Trim();
                order.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }

            if (!changed) return BadRequest(new { message = "No status changes provided" });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated status for order {OrderId} by admin", id);

            // audit
            try { await _audit.WriteAsync("UpdateOrderStatus", nameof(Order), id, $"From={prevOrderStatus} To={order.OrderStatusString}"); } catch { }

            // send optional email on status change
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                var emailTo = user?.Email ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(emailTo))
                {
                    _ = _email.SendTemplateAsync(emailTo, "OrderStatusChanged", new { OrderId = order.Id, NewStatus = order.OrderStatusString });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order status email for order {OrderId}", id);
            }

            // return mapped payload
            var outp = new
            {
                order.Id,
                order.UserId,
                OrderDate = order.OrderDate,
                order.TotalAmount,
                orderStatus = order.OrderStatusString,
                paymentStatus = order.PaymentStatus,
                paymentTransactionId = order.PaymentTransactionId,
                promoCode = order.PromoCode,
                updatedAt = order.UpdatedAt,
                dateDelivered = order.DateDelivered,
                items = order.Items.Select(i => new { i.Id, i.ProductId, i.Quantity, i.UnitPrice })
            };

            return Ok(outp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status {OrderId}", id);
            return StatusCode(500, new { message = "Failed to update order status" });
        }
    }

    // DELETE api/admin/orders/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        _context.OrderItems.RemoveRange(order.Items);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted order {OrderId}", id);
        return NoContent();
    }
}
