using System;

namespace Readify.Models
{
    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        // DiscountPercent used when Type == "Percentage"
        public decimal DiscountPercent { get; set; }
        // Fixed amount discount when Type == "Fixed"
        public decimal? FixedAmount { get; set; }
        // Type: "Percentage" | "Fixed" | "FreeShipping"
        public string? Type { get; set; } = "Percentage";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
