using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using Xunit;
using System.Threading.Tasks;
using System;

public class PurchaseOrderServiceUnitPriceTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        var db = new AppDbContext(opts);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Create_PassUnitPrice_ComputesTotalAmount()
    {
        await using var db = CreateDb();
        var sup = new Supplier { Name = "S1", Email = "s@x.com", Phone = "123", Address = "a", CreatedAt = DateTime.UtcNow };
        db.Suppliers.Add(sup);
        var p1 = new Product { Title = "P1", Price = 5m, StockQty = 1, InitialStock = 1, CategoryId = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(p1);
        await db.SaveChangesAsync();

        var svc = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);
        var po = await svc.CreateAsync(sup.Id, new[] { (p1.Id, 2, 10m) });
        Assert.NotNull(po);
        Assert.Equal(20m, po.TotalAmount);
    }
}
