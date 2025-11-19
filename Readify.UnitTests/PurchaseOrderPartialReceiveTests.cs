using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class PurchaseOrderPartialReceiveTests
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
    public async Task PartialReceive_UpdatesReceivedQuantityAndStock()
    {
        await using var db = CreateDb();
        var sup = new Supplier { Name = "S1", CreatedAt = DateTime.UtcNow };
        db.Suppliers.Add(sup);
        var p = new Product { Title = "P1", Price = 10m, StockQty = 0, InitialStock = 0, CategoryId = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var svc = new PurchaseOrderService(db, NullLogger<PurchaseOrderService>.Instance);
        var po = await svc.CreateAsync(sup.Id, new[] { (p.Id, 10, 9.5m) });
        Assert.NotNull(po);
        Assert.Equal(95m, po.TotalAmount);

        // partially receive 4 units
        var partial = await svc.ReceivePartialAsync(po.Id, new[] { (po.Items.First().Id, 4) });
        Assert.NotNull(partial);
        var item = partial.Items.First();
        Assert.Equal(4, item.ReceivedQuantity);
        var prod = await db.Products.FindAsync(p.Id);
        Assert.Equal(4, prod.StockQty);

        // receive remaining 6
        var final = await svc.ReceivePartialAsync(po.Id, new[] { (po.Items.First().Id, 6) });
        Assert.NotNull(final);
        var finalItem = final.Items.First();
        Assert.Equal(10, finalItem.ReceivedQuantity);
        var prod2 = await db.Products.FindAsync(p.Id);
        Assert.Equal(10, prod2.StockQty);
        // PO status should be 'Received'
        Assert.Equal("Received", final.Status);
    }
}
