using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Readify.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending / Received / Cancelled
        public DateTime? ReceivedAt { get; set; }
        public int? ReceivedByUserId { get; set; }

        // Tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Total cost for the PO (optional, calculated from items)
        public decimal TotalAmount { get; set; } = 0m;

        // navigation
        public Supplier? Supplier { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
}
