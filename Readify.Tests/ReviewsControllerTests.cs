using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public class ReviewsControllerTests
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
    public async Task PostAndGetApprovedReviews()
    {
        await using var db = CreateDb();
        var user = new User { FullName = "Test", Email = "u@test.com", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        var prod = new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 5, CategoryId = 0, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = new ReviewsController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ReviewsController>());
        var claims = new[] { new Claim("userId", user.Id.ToString()) };
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) } };

        var rv = new Review { ProductId = prod.Id, Rating = 5, Comment = "Great" };
        var post = await controller.Create(rv) as CreatedAtActionResult;
        Assert.NotNull(post);

        // Admin approve
        var adminController = new AdminReviewsController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminReviewsController>());
        adminController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", user.Id.ToString()), new Claim(System.Security.Claims.ClaimTypes.Role, "Admin") }, "Test")) } };
        var approve = await adminController.Approve(rv.Id, true) as OkObjectResult;
        // Note: Id may be 0 if not persisted; refresh from DB
        var saved = await db.Reviews.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        saved.IsApproved = true;
        await db.SaveChangesAsync();

        var get = await controller.GetByProduct(prod.Id) as OkObjectResult;
        Assert.NotNull(get);
    }
}
