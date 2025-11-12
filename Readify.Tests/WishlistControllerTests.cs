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

public class WishlistControllerTests
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
    public async Task Add_Get_Remove_Wishlist_Succeeds()
    {
        await using var db = CreateDb();
        var user = new User { FullName = "Test", Email = "u@test.com", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        var prod = new Product { Title = "P", Authors = "A", ISBN = "1", Price = 10m, StockQty = 5, CategoryId = 0, Description = "d", ImageUrl = "i", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = new WishlistController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<WishlistController>());
        var claims = new[] { new Claim("userId", user.Id.ToString()) };
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) } };

        // Add
        var addRes = await controller.Add(prod.Id) as CreatedAtActionResult;
        Assert.NotNull(addRes);

        // Get
        var getRes = await controller.GetMyWishlist() as OkObjectResult;
        Assert.NotNull(getRes);
        var items = getRes.Value as System.Collections.IEnumerable;
        Assert.NotNull(items);

        // Remove
        var del = await controller.Remove(prod.Id) as NoContentResult;
        Assert.NotNull(del);
    }
}
