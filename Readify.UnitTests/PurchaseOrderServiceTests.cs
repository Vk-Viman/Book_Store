using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using System.Linq;
using Xunit;
using System.Threading.Tasks;
using System;

public class PurchaseOrderServiceTests
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
    public async Task CreateAndReceive_PurchaseOrder_UpdatesStock()
    {
        await using var db = CreateDb();
        // seed supplier and product
        var sup = new Supplier { Name = "S1", Email = "s@x.com", Phone = "123", Address = "a", CreatedAt = DateTime.UtcNow };
        db.Suppliers.Add(sup);
        var p = new Product { Title = "P1", Price = 5m, StockQty = 1, InitialStock = 1, CategoryId = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var svc = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);
        var po = await svc.CreateAsync(sup.Id, new[] { (p.Id, 10) });
        Assert.NotNull(po);
        Assert.Equal(1, po.SupplierId);
        Assert.Single(po.Items);

        var after = await svc.ReceiveAsync(po.Id);
        Assert.NotNull(after);
        var prod = await db.Products.FindAsync(p.Id);
        Assert.Equal(11, prod.StockQty); // 1 + 10
    }
}
