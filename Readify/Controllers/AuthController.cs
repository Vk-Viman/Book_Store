using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.DTOs;
using Readify.Helpers;
using Readify.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace Readify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwt;
        private readonly IEmailService _email;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, JwtHelper jwt, IEmailService email, IConfiguration config)
        {
            _context = context;
            _jwt = jwt;
            _email = email;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Basic server-side validation
            var emailAttr = new EmailAddressAttribute();
            if (!emailAttr.IsValid(request.Email))
                return BadRequest(new { code = "InvalidEmail", message = "Invalid email format" });

            if (!IsValidPassword(request.Password))
                return BadRequest(new { code = "WeakPassword", message = "Password does not meet complexity requirements" });

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { code = "EmailExists", message = "Email already registered" });

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Failed to create user in database.", detail = ex.Message });
            }

            // Issue tokens
            var token = _jwt.GenerateToken(user);
            var (refreshToken, refreshEntity) = await CreateRefreshTokenAsync(user.Id);

            // Fire-and-forget welcome email (do not block registration)
            _ = _email.SendTemplateAsync(user.Email, "Welcome", new { user.FullName });

            return Ok(new { token, refresh = refreshToken, user.Email, user.FullName, user.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { code = "InvalidCredentials", message = "Invalid email or password" });

            var token = _jwt.GenerateToken(user);
            var (refreshToken, refreshEntity) = await CreateRefreshTokenAsync(user.Id);

            return Ok(new { token, refresh = refreshToken, user.Email, user.FullName, user.Role });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return BadRequest(new { code = "InvalidToken", message = "Refresh token is required" });

            var hash = ComputeHash(refreshToken);
            var stored = await _context.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == hash);
            if (stored == null || stored.Revoked || stored.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { code = "InvalidToken", message = "Refresh token invalid or expired" });

            // rotate refresh token: revoke old and create new
            stored.Revoked = true;
            var (newRefreshToken, newEntity) = await CreateRefreshTokenAsync(stored.UserId);

            var jwt = _jwt.GenerateToken(stored.User!);
            await _context.SaveChangesAsync();
            return Ok(new { token = jwt, refresh = newRefreshToken });
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return BadRequest(new { code = "InvalidToken", message = "Refresh token is required" });
            var hash = ComputeHash(refreshToken);
            var stored = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == hash);
            if (stored == null) return NotFound(new { code = "NotFound", message = "Refresh token not found" });

            stored.Revoked = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Refresh token revoked" });
        }

        private async Task<(string raw, RefreshToken entity)> CreateRefreshTokenAsync(int userId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var raw = Convert.ToBase64String(tokenBytes);
            var hash = ComputeHash(raw);

            var refresh = new RefreshToken
            {
                UserId = userId,
                Token = hash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Revoked = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refresh);
            await _context.SaveChangesAsync();
            return (raw, refresh);
        }

        private static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // don't reveal whether email exists
                return Ok(new { message = "If the email exists, a reset link has been sent." });
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var expiry = DateTime.UtcNow.AddHours(1);

            var reset = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = expiry,
                Used = false
            };

            _context.PasswordResetTokens.Add(reset);
            await _context.SaveChangesAsync();

            var frontendUrl = _config["Frontend:Url"] ?? "http://localhost:4200";
            var resetLink = $"{frontendUrl}/reset-password/{Uri.EscapeDataString(token)}";

            var body = $"<p>Hello {user.FullName},</p><p>Click <a href=\"{resetLink}\">here</a> to reset your password. This link expires in 1 hour.</p>";
            await _email.SendAsync(user.Email, "Reset your Readify password", body);

            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var reset = await _context.PasswordResetTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == request.Token);
            if (reset == null || reset.Used || reset.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { code = "InvalidToken", message = "Password reset token is invalid or expired" });

            if (!IsValidPassword(request.NewPassword))
                return BadRequest(new { code = "WeakPassword", message = "Password does not meet complexity requirements" });

            var user = reset.User!;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            reset.Used = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { user.Email, user.FullName, user.Role });
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            // At least one letter and one number or special character
            var hasLetter = Regex.IsMatch(password, @"[A-Za-z]");
            var hasNumberOrSpecial = Regex.IsMatch(password, @"[0-9\W]");
            return hasLetter && hasNumberOrSpecial;
        }
    }
}
