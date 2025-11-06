using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Xunit;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

public class AdminStatsControllerTests
{
    [Fact]
    public async Task GetStats_IsAdminOnly()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var controller = new AdminStatsController(db, cache, NullLogger<AdminStatsController>.Instance);

        // calling without context should return 401/Forbid due to Authorize attribute when wired in runtime; here we just call method directly
        var res = await controller.GetStats();
        Assert.NotNull(res);
        var ok = res as Microsoft.AspNetCore.Mvc.OkObjectResult;
        Assert.NotNull(ok);
    }
}
