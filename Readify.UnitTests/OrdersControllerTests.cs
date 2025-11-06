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

public class OrdersControllerTests
{
    [Fact]
    public async Task Checkout_CreatesOrderAndDecrementsStock()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        // seed category, product and user
        var category = new Category { Name = "TestCat" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var user = new User { FullName = "Test", Email = "u@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        db.Products.Add(new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 5, CategoryId = category.Id, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var product = await db.Products.FirstAsync();
        // add cart
        db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 2 });
        await db.SaveChangesAsync();

        var emailLogger = NullLogger<LoggingEmailService>.Instance;
        var ordersLogger = NullLogger<Readify.Controllers.OrdersController>.Instance;
        var email = new LoggingEmailService(emailLogger);
        var shipping = new Readify.UnitTests.TestHelpers.MockShippingService();
        var controller = new Readify.Controllers.OrdersController(db, email, ordersLogger, shipping);

        // set HttpContext user with claim userId to simulate authenticated user
        var claims = new[] { new Claim("userId", user.Id.ToString()), new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()) };
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
