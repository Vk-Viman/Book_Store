using System.ComponentModel.DataAnnotations;

namespace Readify.Models
{
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public string Provider { get; set; } = "Log";
    }
}
