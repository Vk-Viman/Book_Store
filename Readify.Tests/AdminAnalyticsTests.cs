using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers.Admin;
using Readify.Controllers;
using Readify.DTOs;

namespace Readify.Tests
{
    public class AdminAnalyticsTests
    {
        private AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var ctx = new AppDbContext(options);
            return ctx;
        }

        [Fact]
        public async Task Summary_ReturnsTotals()
        {
            var ctx = CreateContext("summary_db");
            // seed users
            ctx.Users.Add(new User { FullName = "A", Email = "a@x.com", PasswordHash = "x", Role = "User", CreatedAt = DateTime.UtcNow.AddDays(-5), IsActive = true });
            ctx.Users.Add(new User { FullName = "B", Email = "b@x.com", PasswordHash = "x", Role = "User", CreatedAt = DateTime.UtcNow.AddDays(-2), IsActive = true });

            // seed categories/products
            var cat = new Category { Name = "Fiction" };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var p1 = new Product { Title = "P1", Authors = "Auth1", Price = 10, StockQty = 5, CategoryId = cat.Id, CreatedAt = DateTime.UtcNow };
            var p2 = new Product { Title = "P2", Authors = "Auth2", Price = 20, StockQty = 3, CategoryId = cat.Id, CreatedAt = DateTime.UtcNow };
            ctx.Products.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            // orders and items
            var o1 = new Order { UserId = 1, OrderDate = DateTime.UtcNow.AddDays(-1), TotalAmount = 30 }; // 10 + 20
            var o2 = new Order { UserId = 1, OrderDate = DateTime.UtcNow.AddDays(-2), TotalAmount = 20 };
            ctx.Orders.AddRange(o1, o2);
            await ctx.SaveChangesAsync();

            ctx.OrderItems.Add(new OrderItem { OrderId = o1.Id, ProductId = p1.Id, Quantity = 1, UnitPrice = 10 });
            ctx.OrderItems.Add(new OrderItem { OrderId = o1.Id, ProductId = p2.Id, Quantity = 1, UnitPrice = 20 });
            ctx.OrderItems.Add(new OrderItem { OrderId = o2.Id, ProductId = p2.Id, Quantity = 1, UnitPrice = 20 });
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<AdminAnalyticsController>.Instance;
            var controller = new AdminAnalyticsController(ctx, cache, logger);

            var sumResult = await controller.GetSummary();
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(sumResult);
            dynamic dto = ok.Value;
            Assert.Equal(2, (int)dto.TotalUsers);
            Assert.Equal(2, (int)dto.TotalOrders);
            Assert.Equal(50m, (decimal)dto.TotalRevenue);
        }

        [Fact]
        public async Task Revenue_ReturnsDailyAggregation()
        {
            var ctx = CreateContext("revenue_db");
            // create orders across 3 days
            var today = DateTime.UtcNow.Date;
            ctx.Orders.Add(new Order { UserId = 1, OrderDate = today.AddDays(-2), TotalAmount = 10 });
            ctx.Orders.Add(new Order { UserId = 1, OrderDate = today.AddDays(-1), TotalAmount = 20 });
            ctx.Orders.Add(new Order { UserId = 2, OrderDate = today.AddDays(-1), TotalAmount = 5 });
            ctx.Orders.Add(new Order { UserId = 2, OrderDate = today, TotalAmount = 15 });
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<AdminAnalyticsController>.Instance;
            var controller = new AdminAnalyticsController(ctx, cache, logger);

            var res = await controller.GetRevenue(5);
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res);
            var dto = Assert.IsType<Readify.DTOs.RevenueDto>(ok.Value);
            // should have entries for dates present
            Assert.Contains(dto.Values, v => v == 10m);
            Assert.Contains(dto.Values, v => v == 25m); // combined 20+5
            Assert.Contains(dto.Values, v => v == 15m);
            Assert.Equal(dto.TotalRevenue, dto.Values.Sum());
        }

        [Fact]
        public async Task TopCategories_ReturnsRevenuePerCategory()
        {
            var ctx = CreateContext("topcat_db");
            var c1 = new Category { Name = "C1" };
            var c2 = new Category { Name = "C2" };
            ctx.Categories.AddRange(c1, c2);
            await ctx.SaveChangesAsync();

            var p1 = new Product { Title = "P1", Authors = "A", Price = 10, CategoryId = c1.Id, CreatedAt = DateTime.UtcNow };
            var p2 = new Product { Title = "P2", Authors = "B", Price = 20, CategoryId = c2.Id, CreatedAt = DateTime.UtcNow };
            ctx.Products.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            var o = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 50 };
            ctx.Orders.Add(o);
            await ctx.SaveChangesAsync();

            ctx.OrderItems.Add(new OrderItem { OrderId = o.Id, ProductId = p1.Id, Quantity = 2, UnitPrice = 10 }); // 20
            ctx.OrderItems.Add(new OrderItem { OrderId = o.Id, ProductId = p2.Id, Quantity = 1, UnitPrice = 20 }); // 20
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<AdminAnalyticsController>.Instance;
            var controller = new AdminAnalyticsController(ctx, cache, logger);

            var res = await controller.GetTopCategories(5);
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res);
            var dto = Assert.IsType<Readify.DTOs.ChartDataDto>(ok.Value);
            Assert.Contains("C1", dto.Labels);
            Assert.Contains("C2", dto.Labels);
            Assert.Contains(20m, dto.Values);
        }

        [Fact]
        public async Task TopAuthors_ReturnsRevenuePerAuthor()
        {
            var ctx = CreateContext("topauth_db");
            var p1 = new Product { Title = "P1", Authors = "AuthorA", Price = 10, CategoryId = 0, CreatedAt = DateTime.UtcNow };
            var p2 = new Product { Title = "P2", Authors = "AuthorB", Price = 20, CategoryId = 0, CreatedAt = DateTime.UtcNow };
            ctx.Products.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            var o = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 50 };
            ctx.Orders.Add(o);
            await ctx.SaveChangesAsync();

            ctx.OrderItems.Add(new OrderItem { OrderId = o.Id, ProductId = p1.Id, Quantity = 1, UnitPrice = 10 });
            ctx.OrderItems.Add(new OrderItem { OrderId = o.Id, ProductId = p2.Id, Quantity = 2, UnitPrice = 20 });
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<AdminAnalyticsController>.Instance;
            var controller = new AdminAnalyticsController(ctx, cache, logger);

            var res = await controller.GetTopAuthors(5);
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res);
            var dto = Assert.IsType<Readify.DTOs.ChartDataDto>(ok.Value);
            Assert.Contains("AuthorA", dto.Labels);
            Assert.Contains("AuthorB", dto.Labels);
        }

        [Fact]
        public async Task UsersTrend_ReturnsCounts()
        {
            var ctx = CreateContext("users_db");
            var today = DateTime.UtcNow.Date;
            ctx.Users.Add(new User { FullName = "U1", Email = "u1@test", PasswordHash = "x", Role = "User", CreatedAt = today.AddDays(-2), IsActive = true });
            ctx.Users.Add(new User { FullName = "U2", Email = "u2@test", PasswordHash = "x", Role = "User", CreatedAt = today.AddDays(-1), IsActive = true });
            ctx.Users.Add(new User { FullName = "U3", Email = "u3@test", PasswordHash = "x", Role = "User", CreatedAt = today.AddDays(-1), IsActive = true });
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<AdminAnalyticsController>.Instance;
            var controller = new AdminAnalyticsController(ctx, cache, logger);

            var res = await controller.GetUserTrend(5);
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res);
            var dto = Assert.IsType<Readify.DTOs.ChartDataDto>(ok.Value);
            Assert.True(dto.Values.Sum() >= 3);
        }

        [Fact]
        public async Task Revenue_Total_Equals_AdminStats_TotalSales()
        {
            var ctx = CreateContext("totals_match_db");

            // seed data: two orders
            var today = DateTime.UtcNow.Date;
            var o1 = new Order { UserId = 1, OrderDate = today.AddDays(-1), TotalAmount = 35m };
            var o2 = new Order { UserId = 2, OrderDate = today, TotalAmount = 15m };
            ctx.Orders.AddRange(o1, o2);
            await ctx.SaveChangesAsync();

            // add order items (optional, but keep consistency)
            ctx.OrderItems.Add(new OrderItem { OrderId = o1.Id, ProductId = 0, Quantity = 1, UnitPrice = 35m });
            ctx.OrderItems.Add(new OrderItem { OrderId = o2.Id, ProductId = 0, Quantity = 1, UnitPrice = 15m });
            await ctx.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());

            var analyticsLogger = NullLogger<AdminAnalyticsController>.Instance;
            var analyticsController = new AdminAnalyticsController(ctx, cache, analyticsLogger);

            var statsLogger = NullLogger<AdminStatsController>.Instance;
            var statsController = new AdminStatsController(ctx, cache, statsLogger);

            // call analytics revenue (cover both dates)
            var revResult = await analyticsController.GetRevenue(10);
            var revOk = Assert.IsType<OkObjectResult>(revResult);
            var revDto = Assert.IsType<Readify.DTOs.RevenueDto>(revOk.Value);

            // call stats
            var statsResult = await statsController.GetStats();
            var statsOk = Assert.IsType<OkObjectResult>(statsResult);
            // AdminStatsController returns DTOs.AdminStatsDto
            var statsDto = Assert.IsType<Readify.DTOs.AdminStatsDto>(statsOk.Value);

            Assert.Equal(statsDto.TotalSales, revDto.TotalRevenue);
            Assert.Equal(50m, revDto.TotalRevenue); // 35 + 15
        }
    }
}
