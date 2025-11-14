using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Xunit;

public class RecommendationsTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        var db = new AppDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Recommendations_Returns_Items_Based_On_Wishlist_Overlap()
    {
        await using var db = CreateDb();
        // create products
        var p1 = new Product { Title = "P1", Price = 10m, StockQty = 5, CategoryId = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var p2 = new Product { Title = "P2", Price = 12m, StockQty = 5, CategoryId = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var p3 = new Product { Title = "P3", Price = 15m, StockQty = 5, CategoryId = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.AddRange(p1, p2, p3);
        var u1 = new User { FullName = "U1", Email = "u1@test", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var u2 = new User { FullName = "U2", Email = "u2@test", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var u3 = new User { FullName = "U3", Email = "u3@test", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(u1, u2, u3);
        await db.SaveChangesAsync();

        // user1 wishlist: p1
        db.Wishlists.Add(new Wishlist { UserId = u1.Id, ProductId = p1.Id });
        // user2 wishlist: p1, p2
        db.Wishlists.Add(new Wishlist { UserId = u2.Id, ProductId = p1.Id });
        db.Wishlists.Add(new Wishlist { UserId = u2.Id, ProductId = p2.Id });
        // user3 wishlist: p1, p3
        db.Wishlists.Add(new Wishlist { UserId = u3.Id, ProductId = p1.Id });
        db.Wishlists.Add(new Wishlist { UserId = u3.Id, ProductId = p3.Id });

        await db.SaveChangesAsync();

        // Now call the same logic as RecommendationsController but in-process here
        var myWishlist = await db.Wishlists.Where(w => w.UserId == u1.Id).Select(w => w.ProductId).ToListAsync();
        var others = await db.Wishlists.Where(w => myWishlist.Contains(w.ProductId) && w.UserId != u1.Id).Select(w => new { w.UserId, w.ProductId }).ToListAsync();
        var coCounts = others.GroupBy(x => x.ProductId).Select(g => new { ProductId = g.Key, Count = g.Count() }).Where(x => !myWishlist.Contains(x.ProductId)).ToList();
        Assert.NotEmpty(coCounts);
        Assert.Contains(coCounts, c => c.ProductId == p2.Id || c.ProductId == p3.Id);
    }
}
