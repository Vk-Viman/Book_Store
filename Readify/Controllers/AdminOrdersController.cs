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
                query = query.Where(o => o.OrderStatus == s || o.PaymentStatus == s || o.Status == s);
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
                    query = query.Where(o => (o.PaymentTransactionId ?? string.Empty).Contains(tq) || (o.PromoCode ?? string.Empty).Contains(tq) || (o.OrderStatus ?? string.Empty).Contains(tq) || (o.PaymentStatus ?? string.Empty).Contains(tq));
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
                    case "status": query = desc ? query.OrderByDescending(o => o.OrderStatus) : query.OrderBy(o => o.OrderStatus); break;
                    default: query = query.OrderByDescending(o => o.OrderDate); break;
                }
            }
            else
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { items, total, page, pageSize });
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
            return Ok(order);
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
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to get user orders" });
        }
    }

    public class UpdateStatusDto { public string? OrderStatus { get; set; } public string? PaymentStatus { get; set; } }

    // PUT api/admin/orders/update-status/{id}
    [HttpPut("update-status/{id}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { message = "Order not found" });

            var changed = false;
            var prevOrderStatus = order.OrderStatus;
            if (!string.IsNullOrWhiteSpace(dto.OrderStatus) && !string.Equals(order.OrderStatus, dto.OrderStatus, StringComparison.OrdinalIgnoreCase))
            {
                order.OrderStatus = dto.OrderStatus!.Trim();
                order.UpdatedAt = DateTime.UtcNow; // track update time
                if (string.Equals(order.OrderStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
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
            try { await _audit.WriteAsync("UpdateOrderStatus", nameof(Order), id, $"From={prevOrderStatus} To={order.OrderStatus}"); } catch { }

            // send optional email on status change
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                var emailTo = user?.Email ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(emailTo))
                {
                    _ = _email.SendTemplateAsync(emailTo, "OrderStatusChanged", new { OrderId = order.Id, NewStatus = order.OrderStatus });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order status email for order {OrderId}", id);
            }

            return Ok(order);
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
