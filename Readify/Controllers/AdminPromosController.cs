using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/[controller]")]
public class PromosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PromosController> _logger;

    public PromosController(AppDbContext context, ILogger<PromosController> logger) => (_context, _logger) = (context, logger);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
    {
        try
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.PromoCodes.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var tq = q.Trim();
                query = query.Where(p => p.Code.Contains(tq));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { items, totalPages, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list promos");
            return StatusCode(500, new { message = "Failed to list promos" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return NotFound(new { message = "Promo not found" });
            return Ok(promo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get promo");
            return StatusCode(500, new { message = "Failed to get promo" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PromoCode promo)
    {
        if (promo == null) return BadRequest(new { message = "Invalid promo" });

        // basic validation
        if (string.IsNullOrWhiteSpace(promo.Code)) return BadRequest(new { message = "Promo code is required" });
        if (string.Equals(promo.Type, "Percentage", StringComparison.OrdinalIgnoreCase) && promo.DiscountPercent <= 0) return BadRequest(new { message = "DiscountPercent must be provided and greater than zero for Percentage promos" });
        if (string.Equals(promo.Type, "Fixed", StringComparison.OrdinalIgnoreCase) && (!promo.FixedAmount.HasValue || promo.FixedAmount <= 0)) return BadRequest(new { message = "FixedAmount must be provided and greater than zero for Fixed promos" });

        try
        {
            // ensure unique code
            var exists = await _context.PromoCodes.AnyAsync(p => p.Code == promo.Code);
            if (exists) return BadRequest(new { message = "Promo code already exists" });

            promo.CreatedAt = DateTime.UtcNow;
            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = promo.Id }, promo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create promo");
            return StatusCode(500, new { message = "Failed to create promo" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PromoCode updated)
    {
        if (updated == null || id != updated.Id) return BadRequest(new { message = "Invalid promo payload" });

        // validation
        if (string.IsNullOrWhiteSpace(updated.Code)) return BadRequest(new { message = "Promo code is required" });
        if (string.Equals(updated.Type, "Percentage", StringComparison.OrdinalIgnoreCase) && updated.DiscountPercent <= 0) return BadRequest(new { message = "DiscountPercent must be provided and greater than zero for Percentage promos" });
        if (string.Equals(updated.Type, "Fixed", StringComparison.OrdinalIgnoreCase) && (!updated.FixedAmount.HasValue || updated.FixedAmount <= 0)) return BadRequest(new { message = "FixedAmount must be provided and greater than zero for Fixed promos" });

        try
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return NotFound(new { message = "Promo not found" });

            // check uniqueness of code if changed
            if (!string.Equals(promo.Code, updated.Code, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _context.PromoCodes.AnyAsync(p => p.Code == updated.Code && p.Id != id);
                if (exists) return BadRequest(new { message = "Promo code already exists" });
            }

            promo.Code = updated.Code;
            promo.Type = updated.Type;
            promo.DiscountPercent = updated.DiscountPercent;
            promo.FixedAmount = updated.FixedAmount;
            promo.IsActive = updated.IsActive;

            _context.PromoCodes.Update(promo);
            await _context.SaveChangesAsync();
            return Ok(promo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update promo");
            return StatusCode(500, new { message = "Failed to update promo" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return NotFound(new { message = "Promo not found" });
            _context.PromoCodes.Remove(promo);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete promo");
            return StatusCode(500, new { message = "Failed to delete promo" });
        }
    }
}
