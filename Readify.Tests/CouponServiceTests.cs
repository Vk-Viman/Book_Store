using System;using System.Threading.Tasks;using Microsoft.EntityFrameworkCore;using Readify.Data;using Readify.Models;using Readify.Services;using Xunit;

public class CouponServiceTests
{
    private AppDbContext CreateDb(){var opts=new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;return new AppDbContext(opts);}    

    [Fact]
    public async Task Validate_Expired_Fails()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="TEST", Type="Percentage", DiscountPercent=10, IsActive=true, ExpiryDate=DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var res = await svc.ValidateAsync("TEST", 1, 100m);
        Assert.False(res.IsValid); Assert.Equal("Code expired", res.Message);
    }

    [Fact]
    public async Task Validate_MinPurchase_Fails()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="MIN", Type="Fixed", FixedAmount=5, MinPurchase=50m, IsActive=true });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var res = await svc.ValidateAsync("MIN", 1, 20m);
        Assert.False(res.IsValid); Assert.Contains("Minimum purchase", res.Message);
    }

    [Fact]
    public async Task Validate_Percentage_Computes_Discount()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="PCT", Type="Percentage", DiscountPercent=15m, IsActive=true });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var res = await svc.ValidateAsync("PCT", 1, 200m);
        Assert.True(res.IsValid); Assert.Equal(30m, res.DiscountAmount);
    }

    [Fact]
    public async Task Validate_Fixed_Computes_Discount_Capped()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="FIX", Type="Fixed", FixedAmount=25m, IsActive=true });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var res = await svc.ValidateAsync("FIX", 1, 10m);
        Assert.True(res.IsValid); Assert.Equal(10m, res.DiscountAmount); // cannot exceed subtotal
    }

    [Fact]
    public async Task Validate_FreeShipping()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="SHIP", Type="FreeShipping", IsActive=true });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var res = await svc.ValidateAsync("SHIP", 1, 80m);
        Assert.True(res.IsValid); Assert.True(res.FreeShipping);
    }

    [Fact]
    public async Task GlobalUsageLimit_Enforced()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="LIMIT", Type="Percentage", DiscountPercent=5m, IsActive=true, GlobalUsageLimit=2, RemainingUses=2 });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var r1 = await svc.ApplyAsync("LIMIT", 1, 100m); Assert.True(r1.IsValid);
        var r2 = await svc.ApplyAsync("LIMIT", 2, 100m); Assert.True(r2.IsValid);
        var r3 = await svc.ValidateAsync("LIMIT", 3, 100m); Assert.False(r3.IsValid); Assert.Equal("Usage limit exceeded", r3.Message);
    }

    [Fact]
    public async Task PerUserLimit_Enforced()
    {
        using var db = CreateDb();
        db.PromoCodes.Add(new PromoCode { Code="USERLIM", Type="Fixed", FixedAmount=5m, IsActive=true, PerUserLimit=1, RemainingUses=null });
        await db.SaveChangesAsync();
        var svc = new CouponService(db);
        var r1 = await svc.ApplyAsync("USERLIM", 5, 50m); Assert.True(r1.IsValid);
        var r2 = await svc.ValidateAsync("USERLIM", 5, 50m); Assert.False(r2.IsValid); Assert.Equal("Per-user usage limit reached", r2.Message);
    }
}
