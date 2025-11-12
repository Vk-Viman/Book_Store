using Readify.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }

    // legacy field; not used for lifecycle but kept for compatibility
    public string Status { get; set; } = "Pending";

    // Payment status still string for flexibility
    public string PaymentStatus { get; set; } = "Pending";

    // Persisted enum as string for OrderStatus
    [Column("OrderStatus")]
    public string OrderStatusString { get; set; } = "Pending";

    [NotMapped]
    public OrderStatus OrderStatus
    {
        get => Enum.TryParse<OrderStatus>(OrderStatusString, true, out var s) ? s : OrderStatus.Pending;
        set => OrderStatusString = value.ToString();
    }

    // Timestamps for lifecycle
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DateDelivered { get; set; }

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
