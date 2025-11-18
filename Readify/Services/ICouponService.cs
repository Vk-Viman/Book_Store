using System.Threading.Tasks;
using Readify.Models;

namespace Readify.Services
{
    public record CouponValidationResult(bool IsValid, string? Message, PromoCode? Promo, decimal DiscountAmount, bool FreeShipping);
    public interface ICouponService
    {
        Task<CouponValidationResult> ValidateAsync(string code, int userId, decimal subtotal);
        Task<CouponValidationResult> ApplyAsync(string code, int userId, decimal subtotal); // same as validate but may reserve usage
        Task<bool> RecordUsageAsync(string code, int userId);
    }
}
