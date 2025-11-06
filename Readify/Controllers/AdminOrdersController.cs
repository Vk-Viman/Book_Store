using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(AppDbContext context, ILogger<AdminOrdersController> logger)
    {
        _context = context;
        _logger = logger;
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
