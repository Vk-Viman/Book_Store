using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;

namespace Readify.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = new
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                RecentAudits = await _context.AuditLogs
                    .Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync()
            };

            return Ok(stats);
        }
    }
}
