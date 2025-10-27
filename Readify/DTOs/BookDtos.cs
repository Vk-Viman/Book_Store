namespace Readify.DTOs
{
    public record BookCreateDto(string Title, string Authors, string Description, int CategoryId, decimal Price, int StockQty, string ImageUrl = "");
    public record BookReadDto(int Id, string Title, string Authors, string Description, int CategoryId, string CategoryName, decimal Price, int StockQty, string ImageUrl);
}
