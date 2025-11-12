using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Readify.Services;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Readify.Tests
{
    public class AdminOrdersControllerTests
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
        public async Task UpdateStatus_InvalidTransition_ReturnsBadRequest()
        {
            await using var db = CreateDb();
            var order = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatusString = "Pending", PaymentStatus = "Pending" };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var controller = new AdminOrdersController(db, NullLogger<AdminOrdersController>.Instance, new LoggingEmailService(NullLogger<LoggingEmailService>.Instance), new NullAuditService());

            var dto = new AdminOrdersController.UpdateStatusDto { OrderStatus = "Delivered" };
            var res = await controller.UpdateStatus(order.Id, dto);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task UpdateStatus_ValidTransition_UpdatesStatus()
        {
            await using var db = CreateDb();
            var order = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatusString = "Processing", PaymentStatus = "Pending" };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var controller = new AdminOrdersController(db, NullLogger<AdminOrdersController>.Instance, new LoggingEmailService(NullLogger<LoggingEmailService>.Instance), new NullAuditService());
            var dto = new AdminOrdersController.UpdateStatusDto { OrderStatus = "Shipped" };
            var res = await controller.UpdateStatus(order.Id, dto) as OkObjectResult;
            Assert.NotNull(res);
            dynamic outp = res.Value!;
            Assert.Equal("Shipped", (string)outp.orderStatus);
        }

        [Fact]
        public async Task UpdateStatus_CannotCancelDelivered_ReturnsBadRequest()
        {
            await using var db = CreateDb();
            var order = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatusString = "Delivered", PaymentStatus = "Paid" };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var controller = new AdminOrdersController(db, NullLogger<AdminOrdersController>.Instance, new LoggingEmailService(NullLogger<LoggingEmailService>.Instance), new NullAuditService());
            var dto = new AdminOrdersController.UpdateStatusDto { OrderStatus = "Cancelled" };
            var res = await controller.UpdateStatus(order.Id, dto);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task UpdateStatus_InvalidStatusString_ReturnsBadRequest()
        {
            await using var db = CreateDb();
            var order = new Order { UserId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 10m, OrderStatusString = "Pending", PaymentStatus = "Pending" };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var controller = new AdminOrdersController(db, NullLogger<AdminOrdersController>.Instance, new LoggingEmailService(NullLogger<LoggingEmailService>.Instance), new NullAuditService());
            var dto = new AdminOrdersController.UpdateStatusDto { OrderStatus = "NotAStatus" };
            var res = await controller.UpdateStatus(order.Id, dto);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        // Simple null audit service used in tests
        private class NullAuditService : IAuditService { public Task WriteAsync(string action, string entity, int entityId, string? details = null) => Task.CompletedTask; }
    }
}
