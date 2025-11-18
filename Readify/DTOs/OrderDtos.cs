using System;
using System.Collections.Generic;

namespace Readify.DTOs
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string? PromoCode { get; set; }
    }

    public class OrderDetailDto : OrderSummaryDto
    {
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public string? ShippingName { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingPhone { get; set; }
    }
}
