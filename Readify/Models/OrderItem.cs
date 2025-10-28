using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}
