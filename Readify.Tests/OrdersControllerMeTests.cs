using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

public class OrdersControllerMeTests
{
    [Fact]
    public async Task GetMyOrders_ReturnsOnlyUserOrders()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var category = new Category { Name = "C" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var user1 = new User { FullName = "U1", Email = "u1@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user2 = new User { FullName = "U2", Email = "u2@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var p = new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 10, CategoryId = category.Id, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var o1 = new Order { UserId = user1.Id, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatus = "Processing", PaymentStatus = "Paid" };
        o1.Items.Add(new OrderItem { ProductId = p.Id, Quantity = 1, UnitPrice = p.Price });
        var o2 = new Order { UserId = user2.Id, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatus = "Processing", PaymentStatus = "Paid" };
        o2.Items.Add(new OrderItem { ProductId = p.Id, Quantity = 1, UnitPrice = p.Price });
        db.Orders.AddRange(o1, o2);
        await db.SaveChangesAsync();

        var controller = new OrdersController(db, new LoggingEmailService(NullLogger<LoggingEmailService>.Instance), NullLogger<OrdersController>.Instance, new MockShippingService());
        var claims = new[] { new Claim("userId", user1.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user1.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

        var res = await controller.GetMyOrders();
        Assert.NotNull(res);
        var ok = res as Microsoft.AspNetCore.Mvc.OkObjectResult;
        Assert.NotNull(ok);
        var list = ok.Value as System.Collections.Generic.List<Readify.DTOs.OrderSummaryDto>;
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal(o1.Id, list.First().Id);
    }
}

public class MockShippingService : IShippingService { public Task<decimal> GetRateAsync(string region, decimal subtotal) => Task.FromResult(1m); }
