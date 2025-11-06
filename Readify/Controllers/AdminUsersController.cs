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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users.Select(u => new { u.Id, u.Email, u.FullName, u.Role, u.IsActive }).ToListAsync();
        return Ok(users);
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
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
        await _context.SaveChangesAsync();
        _logger.LogInformation("Promoted user {UserId} to Admin", id);
        return NoContent();
    }
}
