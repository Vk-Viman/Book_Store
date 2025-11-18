using System;using System.Linq;using System.Threading.Tasks;using Microsoft.EntityFrameworkCore;using Microsoft.AspNetCore.Http;using System.Security.Claims;using Readify.Data;using Readify.Models;using Readify.Services;using Readify.Controllers;using Xunit;using Microsoft.Extensions.Logging.Abstractions;

public class OrdersControllerCouponIntegrationTests
{
    private AppDbContext CreateDb(){var opts=new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;return new AppDbContext(opts);}    

    [Fact]
    public async Task Checkout_Applies_Percentage_Coupon_And_Records_Usage()
    {
        using var db = CreateDb();
        // seed user, product, cart
        var user = new User { FullName="U", Email="u@test.com", PasswordHash="x", Role="User", IsActive=true, CreatedAt=DateTime.UtcNow };
        db.Users.Add(user);
        var cat = new Category { Name="Cat" }; db.Categories.Add(cat); db.SaveChanges();
        var product = new Product { Title="Book", Authors="Auth", ISBN="1", Price=100m, StockQty=10, CategoryId=cat.Id, Description="d", ImageUrl="i", CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow }; db.Products.Add(product); db.SaveChanges();
        db.CartItems.Add(new CartItem { UserId=user.Id, ProductId=product.Id, Quantity=1 });
        // coupon percentage 10% off
        db.PromoCodes.Add(new PromoCode { Code="SAVE10", Type="Percentage", DiscountPercent=10m, IsActive=true, GlobalUsageLimit=5, RemainingUses=5 });
        db.SaveChanges();

        var email = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance);
        var shipping = new ShippingService(db, Microsoft.Extensions.Options.Options.Create(new ShippingOptions()));
        var controller = new OrdersController(db, email, NullLogger<OrdersController>.Instance, shipping);
        var claims = new[] { new Claim("userId", user.Id.ToString()) }; var identity = new ClaimsIdentity(claims, "TestAuth"); controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity), RequestServices = BuildServices(db) } };

        var dto = new Readify.DTOs.CheckoutDto { PromoCode="SAVE10", ShippingName="N", ShippingAddress="A", ShippingPhone="P", Region="local" };
        var result = await controller.Checkout(dto);
        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var order = Assert.IsType<Order>(ok.Value);
        Assert.Equal(90m, order.TotalAmount); // 10% off 100
        Assert.Equal("SAVE10", order.PromoCode);
        // usage recorded
        Assert.Equal(4, db.PromoCodes.First(p=>p.Code=="SAVE10").RemainingUses);
        Assert.Equal(1, db.PromoCodeUsages.Count(u=>u.PromoCodeId==db.PromoCodes.First().Id && u.UserId==user.Id));
    }

    private IServiceProvider BuildServices(AppDbContext db)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IShippingService>(_ => new ShippingService(db, Microsoft.Extensions.Options.Options.Create(new ShippingOptions())));
        services.AddScoped<IEmailService, LoggingEmailService>();
        return services.BuildServiceProvider();
    }
}
