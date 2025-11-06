using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using Xunit;
using System.Security.Claims;

public class OrdersControllerTests
{
    [Fact]
    public async Task Checkout_CreatesOrderAndDecrementsStock()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        // seed product and user
        var user = new User { FullName = "Test", Email = "u@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        db.Products.Add(new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 5, CategoryId = 0, Description = "d", ImageUrl = "i" });
        await db.SaveChangesAsync();

        var product = await db.Products.FirstAsync();
        // add cart
        db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 2 });
        await db.SaveChangesAsync();

        var email = new LoggingEmailService(NullLoggerFactory.Instance.CreateLogger<LoggingEmailService>());
        var shipping = new MockShippingService();
        var controller = new Readify.Controllers.OrdersController(db, email, NullLoggerFactory.Instance.CreateLogger<Readify.Controllers.OrdersController>(), shipping);

        // set HttpContext user with claim userId to simulate authenticated user
        var claims = new[] { new Claim("userId", user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var dto = new Readify.DTOs.CheckoutDto { ShippingName = "Test", ShippingAddress = "Addr", ShippingPhone = "123", Region = "national" };

        var result = await controller.Checkout(dto);
        Assert.NotNull(result);

        var savedOrder = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync();
        Assert.NotNull(savedOrder);
        Assert.Equal(user.Id, savedOrder.UserId);
        Assert.Equal(2, savedOrder.Items.First().Quantity);

        var refreshedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(3, refreshedProduct.StockQty);
    }
}

public class MockShippingService : IShippingService
{
    public Task<decimal> GetRateAsync(string region, decimal subtotal) => Task.FromResult(2m);
}
