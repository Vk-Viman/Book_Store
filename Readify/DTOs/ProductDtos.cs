namespace Readify.DTOs
{
    public class ProductImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public DateTime? ReleaseDate { get; set; }
        public decimal Price { get; set; }
        public int StockQty { get; set; }
        public int InitialStock { get; set; } // expose initial stock baseline
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal? AvgRating { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
    }
}
