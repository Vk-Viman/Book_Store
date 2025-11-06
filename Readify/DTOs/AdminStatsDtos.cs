namespace Readify.DTOs
{
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
    }
}
