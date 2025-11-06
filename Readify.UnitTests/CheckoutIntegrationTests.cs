using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class CheckoutIntegrationTests
{
    [Fact]
    public async Task Checkout_DecrementsStockAndCreatesOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        // seed category, product and user
        var category = new Category { Name = "Cat" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var user = new User { FullName = "Test User", Email = "intuser@test.local", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var product = new Product { Title = "Integration Book", Authors = "Author", ISBN = "I-1", Price = 15.99m, StockQty = 5, CategoryId = category.Id, Description = "desc", ImageUrl = "img", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // add cart item
        db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 2 });
        await db.SaveChangesAsync();

        // create controller with required services
        var email = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);
        var shipping = new IntegrationMockShippingService();
        var controller = new OrdersController(db, email, NullLogger<OrdersController>.Instance, shipping);

        // set HttpContext user with claim userId
        var claims = new[] { new Claim("userId", user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        // provide an empty RequestServices to avoid null reference if used
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };

        var dto = new Readify.DTOs.CheckoutDto { ShippingName = "T", ShippingAddress = "A", ShippingPhone = "P", Region = "national" };

        var result = await controller.Checkout(dto);
        Assert.NotNull(result);

        // verify order exists
        var saved = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.UserId == user.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved.Items.Count);
        Assert.Equal(2, saved.Items.First().Quantity);

        // verify stock decremented
        var refreshed = await db.Products.FindAsync(product.Id);
        Assert.Equal(3, refreshed.StockQty);
    }
}

public class IntegrationMockShippingService : IShippingService { public Task<decimal> GetRateAsync(string region, decimal subtotal) => Task.FromResult(2m); }
