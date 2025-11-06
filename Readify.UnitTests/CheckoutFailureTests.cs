using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class CheckoutFailureTests
{
    [Fact]
    public async Task Checkout_ReturnsBadRequest_WhenStockInsufficient()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var cat = new Category { Name = "C" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var user = new User { FullName = "U", Email = "u@test", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var product = new Product { Title = "P", Authors = "A", ISBN = "1", Price = 9.99m, StockQty = 1, CategoryId = cat.Id, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // cart with qty > stock
        db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 2 });
        await db.SaveChangesAsync();

        var email = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);
        var shipping = new MockShippingService();
        var controller = new OrdersController(db, email, NullLogger<OrdersController>.Instance, shipping);

        // auth
        var claims = new[] { new Claim("userId", user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };

        var dto = new Readify.DTOs.CheckoutDto { ShippingName = "X", ShippingAddress = "A", ShippingPhone = "P", Region = "national" };

        var res = await controller.Checkout(dto);
        var bad = res as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
        Assert.NotNull(bad);
        Assert.Contains("Insufficient stock", bad.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Checkout_ReturnsBadRequest_WhenPaymentDeclined()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var cat = new Category { Name = "C" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var user = new User { FullName = "U", Email = "u2@test", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var product = new Product { Title = "P2", Authors = "A", ISBN = "2", Price = 19.99m, StockQty = 5, CategoryId = cat.Id, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 1 });
        await db.SaveChangesAsync();

        var email = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);
        var shipping = new MockShippingService();
        var controller = new OrdersController(db, email, NullLogger<OrdersController>.Instance, shipping);

        // set HttpContext and register a failing payment service in RequestServices
        var claims = new[] { new Claim("userId", user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentService, FailingPaymentService>();
        var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.RequestServices = provider;
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };

        var dto = new Readify.DTOs.CheckoutDto { ShippingName = "X", ShippingAddress = "A", ShippingPhone = "P", Region = "national" };

        var res = await controller.Checkout(dto);
        var bad = res as Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
        Assert.NotNull(bad);
        Assert.Contains("Payment declined", bad.Value?.ToString() ?? string.Empty);
    }
}

public class MockShippingService : IShippingService { public Task<decimal> GetRateAsync(string region, decimal subtotal) => Task.FromResult(1m); }
public class FailingPaymentService : IPaymentService { public Task<(bool Success, string? TransactionId)> ChargeAsync(decimal amount, string? method = null, string? token = null) => Task.FromResult<(bool, string?)>((false, null)); public Task<bool> RefundAsync(decimal amount, string? transactionId = null) => Task.FromResult(false); }
