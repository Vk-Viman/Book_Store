using Readify.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    // Original subtotal before any discounts or shipping
    public decimal OriginalTotal { get; set; }
    // Final total charged (subtotal - discount + shipping)
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Pending";
    [Column("OrderStatus")]
    public string OrderStatusString { get; set; } = "Pending";
    [NotMapped]
    public OrderStatus OrderStatus
    {
        get => Enum.TryParse<OrderStatus>(OrderStatusString, true, out var s) ? s : OrderStatus.Pending;
        set => OrderStatusString = value.ToString();
    }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DateDelivered { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? PromoCode { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool FreeShipping { get; set; } = false;
    public string? ShippingName { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhone { get; set; }
    public decimal ShippingCost { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}
