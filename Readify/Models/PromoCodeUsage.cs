using System;

namespace Readify.Models
{
    public class PromoCodeUsage
    {
        public int Id { get; set; }
        public int PromoCodeId { get; set; }
        public int UserId { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
