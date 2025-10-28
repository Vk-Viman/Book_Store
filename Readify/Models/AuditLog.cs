using System.ComponentModel.DataAnnotations;

namespace Readify.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty; // Create|Update|Delete
        public string Entity { get; set; } = string.Empty; // Product|Category|...
        public int? EntityId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Details { get; set; }
    }
}
