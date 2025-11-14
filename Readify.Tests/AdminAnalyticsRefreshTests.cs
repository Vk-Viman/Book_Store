using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers.Admin;
using Readify.Data;
using Readify.Models;
using Xunit;

namespace Readify.Tests
{
    public class AdminAnalyticsRefreshTests
    {
        private AppDbContext CreateContext(string name)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options;
            return new AppDbContext(opts);
        }

        [Fact]
        public async Task Refresh_RemovesTrackedKeys()
        {
            var ctx = CreateContext("refresh_db");
            // seed some data and call endpoints to populate cache
            ctx.Users.Add(new User { FullName = "U1", Email = "u1@test", PasswordHash = "x", Role = "User", CreatedAt = DateTime.UtcNow, IsActive = true });
            ctx.Orders.Add(new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 10m });
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<AdminAnalyticsController>.Instance;
            var controller = new AdminAnalyticsController(ctx, cache, logger);

            // prime cache by calling summary
            var res1 = await controller.GetSummary();
            var ok1 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res1);
            // ensure keys list exists in cache
            Assert.True(cache.TryGetValue("analytics:keys", out var keysObj));
            var keys = keysObj as System.Collections.Generic.List<string>;
            Assert.NotNull(keys);
            Assert.Contains("analytics:summary", keys);

            // call refresh
            var res2 = controller.RefreshCache();
            var ok2 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res2);
            // ensure key removed
            Assert.False(cache.TryGetValue("analytics:summary", out _));
            Assert.False(cache.TryGetValue("analytics:keys", out _));
        }
    }
}
