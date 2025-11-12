using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // persisted column
        [Column("Role")]
        public string RoleString { get; set; } = "User";

        // legacy convenience property used throughout the codebase
        [NotMapped]
        public string Role
        {
            get => RoleString;
            set => RoleString = value;
        }

        // enum-backed helper for new code
        [NotMapped]
        public Role RoleEnum
        {
            get => Enum.TryParse<Role>(RoleString, true, out var r) ? r : Models.Role.User;
            set => RoleString = value.ToString();
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
