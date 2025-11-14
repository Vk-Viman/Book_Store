using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Xunit;

public class RecommendationsControllerUnitTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        return db;
    }

    [Fact]
    public async Task GetForMe_Returns_Popular_When_No_Wishlist()
    {
        await using var db = CreateInMemoryDb();
        // seed products
        db.Products.Add(new Product { Id = 1, Title = "P1", Price = 10m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Products.Add(new Product { Id = 2, Title = "P2", Price = 12m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        // seed wishlists belonging to other users to make P2 popular
        db.Wishlists.Add(new Wishlist { UserId = 2, ProductId = 2, CreatedAt = DateTime.UtcNow });
        db.Wishlists.Add(new Wishlist { UserId = 3, ProductId = 2, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var mem = new MemoryCache(new MemoryCacheOptions());
        var controller = new Readify.Controllers.RecommendationsController(db, mem);

        // simulate no wishlist for user 1
        var result = await controller.GetForMe();
        Assert.NotNull(result);
        // result should be OkObjectResult with items containing product 2
        var ok = result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        Assert.NotNull(ok);
        var obj = ok.Value as dynamic;
        var items = obj.items as System.Collections.IEnumerable;
        bool found = false;
        foreach (var it in items) { var id = (int)it.Id; if (id == 2) found = true; }
        Assert.True(found);
    }

    [Fact]
    public async Task GetForMe_Returns_Cooccurrence_Recommendations()
    {
        await using var db = CreateInMemoryDb();
        // products
        var p1 = new Product { Id = 1, Title = "P1", Price = 10m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var p2 = new Product { Id = 2, Title = "P2", Price = 12m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var p3 = new Product { Id = 3, Title = "P3", Price = 15m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Products.AddRange(p1, p2, p3);

        var u1 = new User { Id = 1, FullName = "U1", Email = "u1@test", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var u2 = new User { Id = 2, FullName = "U2", Email = "u2@test", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        var u3 = new User { Id = 3, FullName = "U3", Email = "u3@test", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(u1, u2, u3);

        await db.SaveChangesAsync();

        // user1 wishlist: p1
        db.Wishlists.Add(new Wishlist { UserId = 1, ProductId = 1, CreatedAt = DateTime.UtcNow });
        // user2 wishlist: p1, p2
        db.Wishlists.Add(new Wishlist { UserId = 2, ProductId = 1, CreatedAt = DateTime.UtcNow });
        db.Wishlists.Add(new Wishlist { UserId = 2, ProductId = 2, CreatedAt = DateTime.UtcNow });
        // user3 wishlist: p1, p3
        db.Wishlists.Add(new Wishlist { UserId = 3, ProductId = 1, CreatedAt = DateTime.UtcNow });
        db.Wishlists.Add(new Wishlist { UserId = 3, ProductId = 3, CreatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var mem = new MemoryCache(new MemoryCacheOptions());
        var controller = new Readify.Controllers.RecommendationsController(db, mem);

        // Mimic authenticated user by setting HttpContext.User? The controller reads claims; for unit test we can call internal logic via method extraction but here call GetForMe which expects authentication.
        // Workaround: set controller.ControllerContext with a user claim
        var claims = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("userId", "1") }, "Test"));
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext() { HttpContext = new DefaultHttpContext() { User = claims } };

        var res = await controller.GetForMe();
        var ok = res as Microsoft.AspNetCore.Mvc.OkObjectResult;
        Assert.NotNull(ok);
        var obj = ok.Value as dynamic;
        var items = obj.items as System.Collections.IEnumerable;
        bool found = false; bool any = false;
        foreach (var it in items) { any = true; var id = (int)it.Id; if (id == 2 || id == 3) found = true; }
        Assert.True(any);
        Assert.True(found);
    }
}
