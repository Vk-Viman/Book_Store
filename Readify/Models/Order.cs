namespace Readify.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";

    // Shipping details
    public string? ShippingName { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhone { get; set; }

    // Initialize to avoid nullability issues in EF Include/ThenInclude
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}
