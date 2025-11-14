using System.Collections.Generic;

namespace Readify.DTOs
{
    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Values { get; set; } = new List<decimal>();
    }

    public class RevenueDto : ChartDataDto
    {
        // total revenue for the returned range
        public decimal TotalRevenue { get; set; }
    }

    public class SummaryDto
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
