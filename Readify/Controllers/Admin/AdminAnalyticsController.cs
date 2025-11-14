using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Readify.Data;
using Readify.DTOs;

namespace Readify.Controllers.Admin;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminAnalyticsController> _logger;
    private const string CacheKeyList = "analytics:keys";

    public AdminAnalyticsController(AppDbContext db, IMemoryCache cache, ILogger<AdminAnalyticsController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    private void TrackCacheKey(string key)
    {
        try
        {
            var list = _cache.GetOrCreate(CacheKeyList, entry => new List<string>());
            if (!list.Contains(key))
            {
                list.Add(key);
                // reset the tracked list with a reasonable expiration
                _cache.Set(CacheKeyList, list, TimeSpan.FromMinutes(20));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track analytics cache key {Key}", key);
        }
    }

    // GET api/admin/analytics/revenue?period=30&from=2025-01-01&to=2025-01-31&categoryId=1
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] int period = 30, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int? categoryId = null)
    {
        var cacheKey = $"analytics:revenue:{period}:{from?.ToString("yyyyMMdd") ?? ""}:{to?.ToString("yyyyMMdd") ?? ""}:{categoryId ?? 0}";
        if (_cache.TryGetValue<RevenueDto>(cacheKey, out var cached))
        {
            return Ok(cached);
        }

        var start = from ?? DateTime.UtcNow.Date.AddDays(-Math.Max(1, period - 1));
        var end = to?.Date ?? DateTime.UtcNow.Date;

        var ordersQ = _db.Orders.AsQueryable().Where(o => o.OrderDate.Date >= start.Date && o.OrderDate.Date <= end.Date);

        if (categoryId.HasValue)
        {
            // filter orders by having items in the given category
            ordersQ = ordersQ.Where(o => o.Items.Any(i => i.Product != null && i.Product.CategoryId == categoryId.Value));
        }

        // Aggregate totals by date
        var grouped = await ordersQ
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
            .ToListAsync();

        // Build continuous date range and map totals (zero for missing days)
        var dto = new RevenueDto();
        var days = (end.Date - start.Date).Days + 1;
        for (int i = 0; i < days; i++)
        {
            var d = start.Date.AddDays(i);
            dto.Labels.Add(d.ToString("yyyy-MM-dd"));
            var match = grouped.FirstOrDefault(g => g.Date == d);
            dto.Values.Add(match != null ? match.Total : 0m);
        }

        dto.TotalRevenue = dto.Values.Sum();

        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
        TrackCacheKey(cacheKey);
        return Ok(dto);
    }

    // GET api/admin/analytics/top-categories?top=10&from=...&to=...&categoryId not used here
    [HttpGet("top-categories")]
    public async Task<IActionResult> GetTopCategories([FromQuery] int top = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var cacheKey = $"analytics:topcat:{top}:{from?.ToString("yyyyMMdd") ?? ""}:{to?.ToString("yyyyMMdd") ?? ""}";
        if (_cache.TryGetValue<ChartDataDto>(cacheKey, out var cached)) return Ok(cached);

        var start = from ?? DateTime.MinValue;
        var end = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

        // Join OrderItems -> Products -> Orders to filter by order date and group by category id
        var categorySums = await (from oi in _db.OrderItems
                                  join p in _db.Products on oi.ProductId equals p.Id
                                  join o in _db.Orders on oi.OrderId equals o.Id
                                  where p.CategoryId != 0 && o.OrderDate >= start && o.OrderDate <= end
                                  group new { oi, p } by p.CategoryId into g
                                  select new { CategoryId = g.Key, Revenue = g.Sum(x => x.oi.UnitPrice * x.oi.Quantity) })
                                 .OrderByDescending(x => x.Revenue)
                                 .Take(top)
                                 .ToListAsync();

        var labels = new List<string>();
        var values = new List<decimal>();
        foreach (var item in categorySums)
        {
            var cat = await _db.Categories.FindAsync(item.CategoryId);
            labels.Add(cat?.Name ?? ("Category " + item.CategoryId));
            values.Add(item.Revenue);
        }

        var dto = new ChartDataDto { Labels = labels, Values = values };
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
        TrackCacheKey(cacheKey);
        return Ok(dto);
    }

    // GET api/admin/analytics/top-authors?top=10&from=...&to=...
    [HttpGet("top-authors")]
    public async Task<IActionResult> GetTopAuthors([FromQuery] int top = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var cacheKey = $"analytics:topauth:{top}:{from?.ToString("yyyyMMdd") ?? ""}:{to?.ToString("yyyyMMdd") ?? ""}";
        if (_cache.TryGetValue<ChartDataDto>(cacheKey, out var cached)) return Ok(cached);

        var start = from ?? DateTime.MinValue;
        var end = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

        var authorSums = await (from oi in _db.OrderItems
                                join p in _db.Products on oi.ProductId equals p.Id
                                join o in _db.Orders on oi.OrderId equals o.Id
                                where !string.IsNullOrEmpty(p.Authors) && o.OrderDate >= start && o.OrderDate <= end
                                group new { oi, p } by p.Authors into g
                                select new { Author = g.Key, Revenue = g.Sum(x => x.oi.UnitPrice * x.oi.Quantity) })
                               .OrderByDescending(x => x.Revenue)
                               .Take(top)
                               .ToListAsync();

        var dto = new ChartDataDto { Labels = authorSums.Select(x => x.Author).ToList(), Values = authorSums.Select(x => x.Revenue).ToList() };
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
        TrackCacheKey(cacheKey);
        return Ok(dto);
    }

    // GET api/admin/analytics/users?period=30&from=...&to=...
    [HttpGet("users")]
    public async Task<IActionResult> GetUserTrend([FromQuery] int period = 30, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var cacheKey = $"analytics:users:{period}:{from?.ToString("yyyyMMdd") ?? ""}:{to?.ToString("yyyyMMdd") ?? ""}";
        if (_cache.TryGetValue<ChartDataDto>(cacheKey, out var cached)) return Ok(cached);

        var start = from ?? DateTime.UtcNow.Date.AddDays(-Math.Max(1, period - 1));
        var end = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        var q = await _db.Users
            .Where(u => u.CreatedAt >= start && u.CreatedAt <= end)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dto = new ChartDataDto { Labels = q.Select(x => x.Date.ToString("yyyy-MM-dd")).ToList(), Values = q.Select(x => (decimal)x.Count).ToList() };
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
        TrackCacheKey(cacheKey);
        return Ok(dto);
    }

    // GET api/admin/analytics/summary?from=2025-11-01&to=2025-11-30&categoryId=1
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int? categoryId = null)
    {
        var cacheKey = $"analytics:summary:{from?.ToString("yyyyMMdd") ?? ""}:{to?.ToString("yyyyMMdd") ?? ""}:{categoryId ?? 0}";
        if (_cache.TryGetValue<SummaryDto>(cacheKey, out var cached)) return Ok(cached);

        // If no filters provided, return global totals
        if (from == null && to == null && categoryId == null)
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalOrders = await _db.Orders.CountAsync();
            var totalRevenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var dto = new SummaryDto { TotalUsers = totalUsers, TotalOrders = totalOrders, TotalRevenue = totalRevenue };
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
            TrackCacheKey(cacheKey);
            return Ok(dto);
        }

        // Apply filters to orders
        var start = from ?? DateTime.MinValue;
        var end = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
        var ordersQ = _db.Orders.AsQueryable().Where(o => o.OrderDate >= start && o.OrderDate <= end);
        if (categoryId.HasValue)
        {
            ordersQ = ordersQ.Where(o => o.Items.Any(i => i.Product != null && i.Product.CategoryId == categoryId.Value));
        }

        var totalUsersFiltered = await _db.Users.Where(u => u.CreatedAt >= start && u.CreatedAt <= end).CountAsync();
        var totalOrdersFiltered = await ordersQ.CountAsync();
        var totalRevenueFiltered = await ordersQ.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var dtoFiltered = new SummaryDto { TotalUsers = totalUsersFiltered, TotalOrders = totalOrdersFiltered, TotalRevenue = totalRevenueFiltered };
        _cache.Set(cacheKey, dtoFiltered, TimeSpan.FromMinutes(10));
        TrackCacheKey(cacheKey);
        return Ok(dtoFiltered);
    }

    // POST api/admin/analytics/refresh
    [HttpPost("refresh")]
    public IActionResult RefreshCache()
    {
        try
        {
            if (_cache.TryGetValue<List<string>>(CacheKeyList, out var keys) && keys != null)
            {
                foreach (var k in keys)
                {
                    try { _cache.Remove(k); } catch { }
                }
                _cache.Remove(CacheKeyList);
            }
            return Ok(new { refreshed = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh analytics cache");
            return StatusCode(500, new { refreshed = false, error = ex.Message });
        }
    }
}
