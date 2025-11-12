using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public class AdminUsersControllerTests
{
    [Fact]
    public async Task Update_User_ReturnsNoContent_And_UpdatesFields()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var admin = new User { FullName = "Admin", Email = "admin@test.com", PasswordHash = "x", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow };
        var user = new User { FullName = "User", Email = "user@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(admin, user);
        await db.SaveChangesAsync();

        var controller = new Readify.Controllers.AdminUsersController(db, NullLogger<Readify.Controllers.AdminUsersController>.Instance);
        // set admin principal
        var claims = new[] { new Claim("userId", admin.Id.ToString()), new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var dto = new Readify.Controllers.AdminUsersController.UpdateUserDto { FullName = "User2", Email = "user2@test.com", Role = "User", IsActive = false };
        var res = await controller.Update(user.Id, dto);
        Assert.IsType<NoContentResult>(res);

        var updated = await db.Users.FindAsync(user.Id);
        Assert.Equal("User2", updated.FullName);
        Assert.Equal("user2@test.com", updated.Email);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task CannotDeactivateLastAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var admin = new User { FullName = "Admin", Email = "admin@test.com", PasswordHash = "x", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var controller = new Readify.Controllers.AdminUsersController(db, NullLogger<Readify.Controllers.AdminUsersController>.Instance);
        var claims = new[] { new Claim("userId", admin.Id.ToString()), new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var dto = new Readify.Controllers.AdminUsersController.UpdateUserDto { IsActive = false };
        var res = await controller.Update(admin.Id, dto);
        var bad = res as BadRequestObjectResult;
        Assert.NotNull(bad);
    }
}
