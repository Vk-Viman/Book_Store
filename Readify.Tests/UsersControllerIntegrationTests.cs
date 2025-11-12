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

public class UsersControllerIntegrationTests
{
    [Fact]
    public async Task UpdateMe_InvalidModel_ReturnsBadRequest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new User { FullName = "U", Email = "u@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userService = new Readify.Services.UserService(db);
        var audit = new Readify.Services.AuditService(db, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        var controller = new UsersController(userService, audit, db);

        var claims = new[] { new Claim("userId", user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var req = new Readify.Controllers.UsersController.UpdateProfileRequest { FullName = "", Email = "not-an-email" };
        var res = await controller.UpdateMe(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    [Fact]
    public async Task UpdateMe_ValidModel_UpdatesUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;
        await using var db = new AppDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new User { FullName = "U", Email = "u@test.com", PasswordHash = "x", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userService = new Readify.Services.UserService(db);
        var audit = new Readify.Services.AuditService(db, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        var controller = new UsersController(userService, audit, db);

        var claims = new[] { new Claim("userId", user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var req = new Readify.Controllers.UsersController.UpdateProfileRequest { FullName = "New Name", Email = "new@test.com" };
        var res = await controller.UpdateMe(req);
        Assert.IsType<NoContentResult>(res);

        var updated = await db.Users.FindAsync(user.Id);
        Assert.Equal("New Name", updated.FullName);
        Assert.Equal("new@test.com", updated.Email);
    }
}
