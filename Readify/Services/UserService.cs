using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int id);
        Task UpdateProfileAsync(int id, string fullName, string email);
        Task ChangePasswordAsync(int id, string currentPassword, string newPassword);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        public UserService(AppDbContext db) { _db = db; }

        public Task<User?> GetByIdAsync(int id) => _db.Users.FirstOrDefaultAsync(u => u.Id == id)!;

        public async Task UpdateProfileAsync(int id, string fullName, string email)
        {
            var user = await _db.Users.FindAsync(id) ?? throw new KeyNotFoundException("User not found");
            user.FullName = fullName;
            user.Email = email;
            await _db.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(int id, string currentPassword, string newPassword)
        {
            var user = await _db.Users.FindAsync(id) ?? throw new KeyNotFoundException("User not found");
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) throw new InvalidOperationException("Current password incorrect");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();
        }
    }
}
