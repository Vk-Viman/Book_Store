using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Readify.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        [Required]
        [MaxLength(1024)]
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}
