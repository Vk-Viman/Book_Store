using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using System.ComponentModel.DataAnnotations;

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

    // GET api/admin/users?page=1&pageSize=20&q=term&sortBy=email&sortDir=asc
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var tq = q.Trim();
            query = query.Where(u => u.Email.Contains(tq) || u.FullName.Contains(tq) || u.Role.Contains(tq));
        }

        // Sorting
        var dirDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            switch (sortBy.ToLowerInvariant())
            {
                case "email": query = dirDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email); break;
                case "name": query = dirDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName); break;
                case "role": query = dirDesc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role); break;
                case "createdat": query = dirDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt); break;
                default: query = query.OrderBy(u => u.Id); break;
            }
        }
        else
        {
            query = query.OrderBy(u => u.Id);
        }

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.CreatedAt })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    // GET api/admin/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Email, user.FullName, user.Role, user.IsActive, user.CreatedAt });
    }

    public class UpdateUserDto
    {
        [StringLength(200, MinimumLength = 2)]
        public string? FullName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be 'Admin' or 'User'.")]
        public string? Role { get; set; }

        public bool? IsActive { get; set; }
    }

    // PUT api/admin/users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // If changing IsActive from true->false for an admin, ensure another active admin remains
        if (dto.IsActive.HasValue && user.IsActive && !dto.IsActive.Value && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var otherActiveAdmins = await _context.Users.AnyAsync(u => u.Id != id && u.IsActive && u.Role == "Admin");
            if (!otherActiveAdmins)
            {
                return BadRequest(new { message = "Cannot deactivate the last active admin." });
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Role)) user.Role = dto.Role.Trim();
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin updated user {UserId}", id);
        return NoContent();
    }

    // DELETE api/admin/users/{id} -> soft delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Prevent deleting last admin
        if (user.IsActive && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var otherActiveAdmins = await _context.Users.AnyAsync(u => u.Id != id && u.IsActive && u.Role == "Admin");
            if (!otherActiveAdmins)
            {
                return BadRequest(new { message = "Cannot deactivate the last active admin." });
            }
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Soft-deleted (deactivated) user {UserId}", id);
        return NoContent();
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
