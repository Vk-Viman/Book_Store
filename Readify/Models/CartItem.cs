using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models;

public class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;

    // Navigation
    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}
