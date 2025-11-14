using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public class AdminReviewsBulkTests
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
    public async Task BulkApprove_UpdatesReviews_RecomputesAvg_And_InvalidatesCache()
    {
        await using var db = CreateDb();

        // create product, users, wishlist
        var prod = new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 5, CategoryId = 0, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(prod);
        var u1 = new User { FullName = "U1", Email = "u1@test.com", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var u2 = new User { FullName = "U2", Email = "u2@test.com", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(u1, u2);
        await db.SaveChangesAsync();

        // reviews (both unapproved)
        var r1 = new Review { ProductId = prod.Id, UserId = u1.Id, Rating = 5, Comment = "Great", CreatedAt = DateTime.UtcNow, IsApproved = false };
        var r2 = new Review { ProductId = prod.Id, UserId = u2.Id, Rating = 3, Comment = "Ok", CreatedAt = DateTime.UtcNow, IsApproved = false };
        db.Reviews.AddRange(r1, r2);

        // wishlist entry to ensure cache invalidation path runs
        db.Wishlists.Add(new Wishlist { UserId = u1.Id, ProductId = prod.Id });
        await db.SaveChangesAsync();

        // pre-populate cache for user u1
        var memory = new MemoryCache(new MemoryCacheOptions());
        var cacheKey = $"recommendations:user:{u1.Id}";
        memory.Set(cacheKey, new { items = new object[] { new { id = 999 } } }, TimeSpan.FromMinutes(60));

        var controller = new AdminReviewsController(db, new NullLogger<AdminReviewsController>(), memory);
        // set admin claims
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", u1.Id.ToString()), new Claim(ClaimTypes.Role, "Admin") }, "TestAuth")) } };

        // Prepare request: approve both reviews
        var req = new AdminReviewsController.BulkApproveRequest { Ids = new System.Collections.Generic.List<int> { r1.Id, r2.Id }, Approve = true };

        var res = await controller.BulkUpdate(req) as OkObjectResult;
        Assert.NotNull(res);
        Assert.Equal(2, ((dynamic)res.Value).updated);

        // verify DB updated
        var saved = await db.Reviews.Where(r => r.ProductId == prod.Id).ToListAsync();
        Assert.All(saved, r => Assert.True(r.IsApproved));

        // verify AvgRating updated on product (average of 5 and 3 = 4.00)
        var refreshed = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prod.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(4.00m, refreshed.AvgRating);

        // verify cache invalidated for user u1
        Assert.False(memory.TryGetValue(cacheKey, out _));
    }
}
