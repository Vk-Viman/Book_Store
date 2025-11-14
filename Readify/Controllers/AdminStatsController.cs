using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Readify.Data;
using Readify.DTOs;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = "Admin")]
public class AdminStatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminStatsController> _logger;

    public AdminStatsController(AppDbContext db, IMemoryCache cache, ILogger<AdminStatsController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var dto = await _cache.GetOrCreateAsync("admin_stats", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                var totalUsers = await _db.Users.CountAsync();
                var totalOrders = await _db.Orders.CountAsync();
                // Use Orders.TotalAmount to match analytics revenue computation
                var totalSales = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
                return new DTOs.AdminStatsDto { TotalUsers = totalUsers, TotalOrders = totalOrders, TotalSales = totalSales };
            });
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute admin stats");
            return StatusCode(500, new { message = "Failed to compute stats" });
        }
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts()
    {
        try
        {
            var list = await _cache.GetOrCreateAsync("admin_top_products", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                var q = await _db.OrderItems
                    .Include(i => i.Product)
                    .GroupBy(i => new { i.ProductId, i.Product!.Title })
                    .Select(g => new DTOs.TopProductDto { ProductName = g.Key.Title, QuantitySold = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.QuantitySold)
                    .Take(5)
                    .ToListAsync();
                return q;
            });
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute top products");
            return StatusCode(500, new { message = "Failed to compute top products" });
        }
    }
}
