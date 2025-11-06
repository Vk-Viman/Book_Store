using System.Threading.Tasks;

namespace Readify.Services
{
    public interface IShippingService
    {
        /// <summary>
        /// Get shipping rate for a given region and order subtotal.
        /// Region expected values: "local", "national", "international" (case-insensitive)
        /// </summary>
        Task<decimal> GetRateAsync(string? region, decimal subtotal);
    }
}
