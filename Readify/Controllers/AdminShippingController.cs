using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/shipping")]
public class AdminShippingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminShippingController> _logger;

    public AdminShippingController(AppDbContext context, ILogger<AdminShippingController> logger) => (_context, _logger) = (context, logger);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var setting = await _context.ShippingSettings.OrderByDescending(s => s.UpdatedAt).FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new ShippingSetting { Local = 2m, National = 5m, International = 15m, FreeShippingThreshold = 100m };
        }
        return Ok(setting);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] ShippingSetting model)
    {
        if (model == null) return BadRequest(new { message = "Invalid payload" });
        try
        {
            // simply append a new row for audit/history and return latest
            model.UpdatedAt = DateTime.UtcNow;
            _context.ShippingSettings.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save shipping settings");
            return StatusCode(500, new { message = "Failed to save shipping settings" });
        }
    }
}
