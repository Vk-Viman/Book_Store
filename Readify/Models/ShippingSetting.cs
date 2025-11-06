namespace Readify.Models
{
    public class ShippingSetting
    {
        public int Id { get; set; }
        public decimal Local { get; set; }
        public decimal National { get; set; }
        public decimal International { get; set; }
        public decimal FreeShippingThreshold { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}