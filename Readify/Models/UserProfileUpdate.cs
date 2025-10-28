using System.ComponentModel.DataAnnotations;

namespace Readify.Models
{
    public class UserProfileUpdate
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OldFullName { get; set; } = string.Empty;
        public string OldEmail { get; set; } = string.Empty;
        public string NewFullName { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
