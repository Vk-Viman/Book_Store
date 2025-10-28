using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Readify.Services;
using Readify.Data;
using Readify.Models;
using Microsoft.EntityFrameworkCore;

namespace Readify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;
        private readonly IAuditService _audit;
        private readonly AppDbContext _db;
        public UsersController(IUserService users, IAuditService audit, AppDbContext db)
        {
            _users = users; _audit = audit; _db = db;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var uidStr = User.FindFirst("userId")?.Value;
            if (!int.TryParse(uidStr, out var uid)) return Unauthorized();
            var u = await _users.GetByIdAsync(uid);
            if (u == null) return NotFound();
            return Ok(new { u.Email, u.FullName, u.Role });
        }

        public class UpdateProfileRequest { public string FullName { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; }
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest req)
        {
            var uidStr = User.FindFirst("userId")?.Value; if (!int.TryParse(uidStr, out var uid)) return Unauthorized();
            // capture old values for profile update audit
            var old = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
            await _users.UpdateProfileAsync(uid, req.FullName, req.Email);
            await _audit.WriteAsync("UpdateProfile", nameof(User), uid, $"Name={req.FullName}");
            if (old != null)
            {
                _db.UserProfileUpdates.Add(new UserProfileUpdate
                {
                    UserId = uid,
                    OldFullName = old.FullName,
                    OldEmail = old.Email,
                    NewFullName = req.FullName,
                    NewEmail = req.Email,
                    UpdatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            return NoContent();
        }

        public class ChangePasswordRequest { public string CurrentPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var uidStr = User.FindFirst("userId")?.Value; if (!int.TryParse(uidStr, out var uid)) return Unauthorized();
            try
            {
                await _users.ChangePasswordAsync(uid, req.CurrentPassword, req.NewPassword);
                await _audit.WriteAsync("ChangePassword", nameof(User), uid);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
