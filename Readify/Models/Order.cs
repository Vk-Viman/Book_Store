using Readify.Helpers;

namespace Readify.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";

    // New fields for Phase 5: payment and order lifecycle statuses
    // Use strings for backward compatibility with existing frontend; values: Pending/Paid/Failed and Processing/Shipped/Delivered/Cancelled
    public string PaymentStatus { get; set; } = "Pending";
    public string OrderStatus { get; set; } = "Processing";

    // store payment transaction id returned by payment provider (optional)
    public string? PaymentTransactionId { get; set; }

    // Applied promo (optional)
    public string? PromoCode { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool FreeShipping { get; set; } = false;

    // Shipping details
    public string? ShippingName { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhone { get; set; }
    public decimal ShippingCost { get; set; }

    // Initialize to avoid nullability issues in EF Include/ThenInclude
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}
