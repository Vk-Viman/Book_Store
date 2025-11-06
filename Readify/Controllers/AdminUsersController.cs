using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(AppDbContext context, ILogger<AdminUsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/admin/users?page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _context.Users.AsNoTracking().OrderBy(u => u.Id);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.FullName, u.Role, u.IsActive })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // If deactivating an admin, ensure at least one other active admin remains
        if (user.IsActive && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var otherActiveAdmins = await _context.Users.AnyAsync(u => u.Id != id && u.IsActive && u.Role == "Admin");
            if (!otherActiveAdmins)
            {
                return BadRequest(new { message = "Cannot deactivate the last active admin." });
            }
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Toggled active state for user {UserId} to {State}", id, user.IsActive);
        return NoContent();
    }

    [HttpPut("{id}/promote")]
    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = "Admin";
        user.IsActive = true; // ensure admin accounts are active
        await _context.SaveChangesAsync();
        _logger.LogInformation("Promoted user {UserId} to Admin", id);
        return NoContent();
    }
}
