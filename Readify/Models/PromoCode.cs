using System;

namespace Readify.Models
{
    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // unique uppercase
        public decimal DiscountPercent { get; set; } // used when Type == Percentage
        public decimal? FixedAmount { get; set; } // used when Type == Fixed
        public string? Type { get; set; } = "Percentage"; // Percentage | Fixed | FreeShipping
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public decimal? MinPurchase { get; set; }
        public int? GlobalUsageLimit { get; set; }
        public int? PerUserLimit { get; set; }
        public int? RemainingUses { get; set; } // if null unlimited; initialize equal to GlobalUsageLimit when created
    }
}
