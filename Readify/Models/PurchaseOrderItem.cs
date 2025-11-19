using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models
{
    public class PurchaseOrderItem
    {
        [Key]
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Optional unit price at time of ordering
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0m;

        // Quantity actually received (useful for partial receipts)
        public int ReceivedQuantity { get; set; } = 0;

        // Navigation back to PurchaseOrder
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        // Navigation to Product
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}
