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

public class OrderHistoryControllerTests
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
    public async Task History_AppendedOnStatusChange()
    {
        await using var db = CreateDb();
        var user = new User { FullName = "U", Email = "u@test.com", PasswordHash = "x", RoleString = "User", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        var order = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatusString = "Pending", PaymentStatus = "Pending" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var admin = new AdminOrdersController(db, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminOrdersController>(), new Readify.Services.LoggingEmailService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<Readify.Services.LoggingEmailService>()), new Readify.Services.NullAuditService());
        var claims = new[] { new Claim("userId", user.Id.ToString()), new Claim(System.Security.Claims.ClaimTypes.Role, "Admin") };
        admin.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) } };

        var dto = new AdminOrdersController.UpdateStatusDto { OrderStatus = "Processing" };
        var res = await admin.UpdateStatus(order.Id, dto);
        Assert.IsType<OkObjectResult>(res);

        var history = await db.OrderHistories.FirstOrDefaultAsync(h => h.OrderId == order.Id);
        Assert.NotNull(history);
    }
}
