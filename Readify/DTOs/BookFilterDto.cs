namespace Readify.DTOs
{
    public class BookFilterDto
    {
        public string? Q { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } // title|price|createdAt
        public string? SortDir { get; set; } // asc|desc
    }
}
