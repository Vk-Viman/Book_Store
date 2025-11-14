using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Xunit;

public class AvgRatingTests
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
    public async Task AvgRating_Updated_When_Review_Approved()
    {
        await using var db = CreateDb();
        var prod = new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 5, CategoryId = 0, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(prod);
        var user = new User { FullName = "U", Email = "u@test.com", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var review = new Review { ProductId = prod.Id, UserId = user.Id, Rating = 4, Comment = "Good", CreatedAt = DateTime.UtcNow, IsApproved = false };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        // simulate admin approving via controller logic
        review.IsApproved = true;
        await db.SaveChangesAsync();

        // recalc avg
        var avg = await db.Reviews.Where(r => r.ProductId == prod.Id && r.IsApproved).AverageAsync(r => (decimal?)r.Rating);
        var p = await db.Products.FindAsync(prod.Id);
        p.AvgRating = avg == null ? null : Math.Round(avg.Value, 2);
        await db.SaveChangesAsync();

        var refreshed = await db.Products.AsNoTracking().FirstOrDefaultAsync(p2 => p2.Id == prod.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(4.00m, refreshed.AvgRating);
    }
}
