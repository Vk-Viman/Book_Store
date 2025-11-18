using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.Services;

namespace Readify.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
// Support both /api/admin/promos and /api/admin/coupons as controller base routes
[Route("api/admin/promos")]
[Route("api/admin/coupons")]
public class PromosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PromosController> _logger;
    private readonly IAuditService _audit;

    public PromosController(AppDbContext context, ILogger<PromosController> logger, IAuditService audit)
        => (_context, _logger, _audit) = (context, logger, audit);

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
                var tq = q.Trim().ToUpperInvariant();
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

        // normalize and basic validation
        promo.Code = (promo.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(promo.Code)) return BadRequest(new { message = "Promo code is required" });
        if (string.Equals(promo.Type, "Percentage", StringComparison.OrdinalIgnoreCase) && promo.DiscountPercent <= 0) return BadRequest(new { message = "DiscountPercent must be provided and greater than zero for Percentage promos" });
        if (string.Equals(promo.Type, "Fixed", StringComparison.OrdinalIgnoreCase) && (!promo.FixedAmount.HasValue || promo.FixedAmount <= 0)) return BadRequest(new { message = "FixedAmount must be provided and greater than zero for Fixed promos" });

        try
        {
            // ensure unique code (case-insensitive)
            var exists = await _context.PromoCodes.AnyAsync(p => p.Code == promo.Code);
            if (exists) return BadRequest(new { message = "Promo code already exists" });

            promo.CreatedAt = DateTime.UtcNow;
            if (promo.GlobalUsageLimit.HasValue && promo.GlobalUsageLimit.Value > 0 && !promo.RemainingUses.HasValue)
            {
                promo.RemainingUses = promo.GlobalUsageLimit.Value;
            }
            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();

            // audit
            await _audit.WriteAsync("PromoCreated", nameof(PromoCode), promo.Id, $"Code={promo.Code}; Type={promo.Type}; Active={promo.IsActive}");

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
        if (string.IsNullOrWhiteSpace(updated.Code)) return BadRequest(new { message = "Promo code is required" });
        if (string.Equals(updated.Type, "Percentage", StringComparison.OrdinalIgnoreCase) && updated.DiscountPercent <= 0) return BadRequest(new { message = "DiscountPercent must be greater than zero" });
        if (string.Equals(updated.Type, "Fixed", StringComparison.OrdinalIgnoreCase) && (!updated.FixedAmount.HasValue || updated.FixedAmount <= 0)) return BadRequest(new { message = "FixedAmount must be greater than zero" });
        try
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return NotFound(new { message = "Promo not found" });
            var newCode = updated.Code.Trim().ToUpperInvariant();
            if (!string.Equals(promo.Code, newCode, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _context.PromoCodes.AnyAsync(p => p.Code == newCode && p.Id != id);
                if (exists) return BadRequest(new { message = "Promo code already exists" });
                promo.Code = newCode;
            }
            bool limitChanged = promo.GlobalUsageLimit != updated.GlobalUsageLimit;
            promo.Type = updated.Type;
            promo.DiscountPercent = updated.DiscountPercent;
            promo.FixedAmount = updated.FixedAmount;
            promo.IsActive = updated.IsActive;
            promo.ExpiryDate = updated.ExpiryDate;
            promo.MinPurchase = updated.MinPurchase;
            promo.GlobalUsageLimit = updated.GlobalUsageLimit;
            promo.PerUserLimit = updated.PerUserLimit;
            if (limitChanged)
            {
                promo.RemainingUses = (promo.GlobalUsageLimit.HasValue && promo.GlobalUsageLimit.Value > 0) ? promo.GlobalUsageLimit : (int?)null;
            }
            if (updated.RemainingUses.HasValue && !limitChanged && promo.GlobalUsageLimit.HasValue && promo.GlobalUsageLimit.Value > 0)
            {
                // clamp manual adjustment to global limit
                var newRemain = Math.Min(promo.GlobalUsageLimit.Value, Math.Max(0, updated.RemainingUses.Value));
                promo.RemainingUses = newRemain;
            }
            _context.PromoCodes.Update(promo);
            await _context.SaveChangesAsync();

            await _audit.WriteAsync("PromoUpdated", nameof(PromoCode), promo.Id, $"Code={promo.Code}; Type={promo.Type}; Active={promo.IsActive}");

            return Ok(promo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update promo");
            return StatusCode(500, new { message = "Failed to update promo" });
        }
    }

    [HttpPatch("{id}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] bool active)
    {
        try
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return NotFound(new { message = "Promo not found" });
            promo.IsActive = active;
            _context.PromoCodes.Update(promo);
            await _context.SaveChangesAsync();
            await _audit.WriteAsync("PromoToggled", nameof(PromoCode), promo.Id, $"Code={promo.Code}; Active={promo.IsActive}");
            return Ok(new { promo.Id, promo.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set active for promo {Id}", id);
            return StatusCode(500, new { message = "Failed to set active state" });
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
            await _audit.WriteAsync("PromoDeleted", nameof(PromoCode), promo.Id, $"Code={promo.Code}");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete promo");
            return StatusCode(500, new { message = "Failed to delete promo" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        try
        {
            var codes = await _context.PromoCodes.AsNoTracking().ToListAsync();
            var usage = await _context.PromoCodeUsages.GroupBy(u => u.PromoCodeId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
            var map = usage.ToDictionary(x => x.Key, x => x.Count);
            var payload = codes.Select(c => new
            {
                c.Id,
                c.Code,
                c.Type,
                c.DiscountPercent,
                c.FixedAmount,
                c.IsActive,
                c.ExpiryDate,
                c.MinPurchase,
                c.GlobalUsageLimit,
                c.PerUserLimit,
                c.RemainingUses,
                totalUsed = map.ContainsKey(c.Id) ? map[c.Id] : 0,
                usagePercent = (c.GlobalUsageLimit.HasValue && c.GlobalUsageLimit.Value > 0 && map.ContainsKey(c.Id)) ? Math.Round((decimal)map[c.Id] / c.GlobalUsageLimit.Value * 100m, 2) : (decimal?)null
            });
            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get promo stats");
            return StatusCode(500, new { message = "Failed to get promo stats" });
        }
    }

    [HttpPatch("{id}/remaining")]
    public async Task<IActionResult> PatchRemaining(int id, [FromBody] int remaining)
    {
        try
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return NotFound(new { message = "Promo not found" });
            if (!promo.GlobalUsageLimit.HasValue || promo.GlobalUsageLimit.Value <= 0) return BadRequest(new { message = "Cannot set remaining for unlimited promo" });
            var clamped = Math.Min(promo.GlobalUsageLimit.Value, Math.Max(0, remaining));
            promo.RemainingUses = clamped;
            _context.PromoCodes.Update(promo);
            await _context.SaveChangesAsync();
            await _audit.WriteAsync("PromoRemainingSet", nameof(PromoCode), promo.Id, $"Code={promo.Code}; Remaining={clamped}");
            return Ok(new { promo.Id, promo.Code, promo.RemainingUses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to patch remaining uses for promo {Id}", id);
            return StatusCode(500, new { message = "Failed to patch remaining uses" });
        }
    }
}
