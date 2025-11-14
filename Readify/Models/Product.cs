using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public DateTime? ReleaseDate { get; set; }
        public decimal Price { get; set; }
        public int StockQty { get; set; }
        public int InitialStock { get; set; } = 0; // newly added: initial stock snapshot
        public int CategoryId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }

        // cached average rating (1-5) based on approved reviews
        [Column(TypeName = "decimal(3,2)")]
        public decimal? AvgRating { get; set; }

        // Concurrency token to protect stock updates
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public Category? Category { get; set; }
    }
}
