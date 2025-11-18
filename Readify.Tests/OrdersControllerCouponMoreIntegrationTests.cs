using System;using System.Linq;using System.Threading.Tasks;using Microsoft.EntityFrameworkCore;using Microsoft.AspNetCore.Http;using System.Security.Claims;using Readify.Data;using Readify.Models;using Readify.Services;using Readify.Controllers;using Xunit;using Microsoft.Extensions.Logging.Abstractions;

public class OrdersControllerCouponMoreIntegrationTests
{
    private AppDbContext CreateDb(){var opts=new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;return new AppDbContext(opts);}    

    private IServiceProvider BuildServices(AppDbContext db)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IShippingService>(_ => new ShippingService(db, Microsoft.Extensions.Options.Options.Create(new ShippingOptions())));
        services.AddScoped<IEmailService, LoggingEmailService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Checkout_Fixed_Coupon_DiscountApplied()
    {
        using var db = CreateDb();
        SeedBasic(db, out var user, out var product);
        db.CartItems.Add(new CartItem { UserId=user.Id, ProductId=product.Id, Quantity=1 });
        db.PromoCodes.Add(new PromoCode { Code="FIX5", Type="Fixed", FixedAmount=5m, IsActive=true });
        db.SaveChanges();
        var controller = CreateController(db, user.Id);
        var dto = new Readify.DTOs.CheckoutDto { PromoCode="FIX5", ShippingName="N", ShippingAddress="A", ShippingPhone="P" };
        var res = await controller.Checkout(dto); var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res); var order = Assert.IsType<Order>(ok.Value);
        Assert.Equal(95m, order.TotalAmount); Assert.Equal(5m, order.DiscountAmount); Assert.Equal("FIX5", order.PromoCode);
    }

    [Fact]
    public async Task Checkout_FreeShipping_Coupon_Applies_FreeShipping()
    {
        using var db = CreateDb();
        SeedBasic(db, out var user, out var product);
        db.CartItems.Add(new CartItem { UserId=user.Id, ProductId=product.Id, Quantity=1 });
        db.PromoCodes.Add(new PromoCode { Code="FREESHIP", Type="FreeShipping", IsActive=true });
        db.SaveChanges();
        var controller = CreateController(db, user.Id);
        var dto = new Readify.DTOs.CheckoutDto { PromoCode="FREESHIP", ShippingName="N", ShippingAddress="A", ShippingPhone="P" };
        var res = await controller.Checkout(dto); var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res); var order = Assert.IsType<Order>(ok.Value);
        Assert.True(order.FreeShipping); Assert.Equal("FREESHIP", order.PromoCode);
    }

    [Fact]
    public async Task Checkout_PerUserLimit_Exceeded_FailsToApply()
    {
        using var db = CreateDb();
        SeedBasic(db, out var user, out var product);
        // coupon with per-user limit 1
        db.PromoCodes.Add(new PromoCode { Code="ONCE", Type="Percentage", DiscountPercent=50m, IsActive=true, PerUserLimit=1 });
        db.SaveChanges();
        // first usage
        db.CartItems.Add(new CartItem { UserId=user.Id, ProductId=product.Id, Quantity=1 }); db.SaveChanges();
        var controller1 = CreateController(db, user.Id);
        var dto = new Readify.DTOs.CheckoutDto { PromoCode="ONCE", ShippingName="N", ShippingAddress="A", ShippingPhone="P" };
        var res1 = await controller1.Checkout(dto); Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res1);
        // second attempt should not apply discount (Validate will block usage)
        db.CartItems.Add(new CartItem { UserId=user.Id, ProductId=product.Id, Quantity=1 }); db.SaveChanges();
        var controller2 = CreateController(db, user.Id);
        var res2 = await controller2.Checkout(dto); var ok2 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(res2); var order2 = Assert.IsType<Order>(ok2.Value);
        Assert.NotEqual("ONCE", order2.PromoCode); // not applied
        Assert.Equal(0m, order2.DiscountAmount);
    }

    private void SeedBasic(AppDbContext db, out User user, out Product product)
    {
        user = new User { FullName="U", Email="u@test.com", PasswordHash="x", Role="User", IsActive=true, CreatedAt=DateTime.UtcNow };
        db.Users.Add(user);
        var cat = new Category { Name="Cat" }; db.Categories.Add(cat); db.SaveChanges();
        product = new Product { Title="Book", Authors="Auth", ISBN="1", Price=100m, StockQty=50, CategoryId=cat.Id, Description="d", ImageUrl="i", CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow };
        db.Products.Add(product); db.SaveChanges();
    }

    private OrdersController CreateController(AppDbContext db, int userId)
    {
        var email = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);
        var shipping = new ShippingService(db, Microsoft.Extensions.Options.Options.Create(new ShippingOptions()));
        var controller = new OrdersController(db, email, NullLogger<OrdersController>.Instance, shipping);
        var claims = new[] { new Claim("userId", userId.ToString()) }; var identity = new ClaimsIdentity(claims, "TestAuth"); controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity), RequestServices = BuildServices(db) } };
        return controller;
    }
}
