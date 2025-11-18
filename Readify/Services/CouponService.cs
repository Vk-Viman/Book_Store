using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Readify.Services
{
    public class CouponService : ICouponService
    {
        private readonly AppDbContext _db;
        private readonly object _lock = new();
        public CouponService(AppDbContext db) { _db = db; }

        public async Task<CouponValidationResult> ValidateAsync(string code, int userId, decimal subtotal)
        {
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code)) return new(false, "Code required", null, 0m, false);
            var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Code == code);
            if (promo == null || !promo.IsActive) return new(false, "Invalid or inactive code", null, 0m, false);
            if (promo.ExpiryDate.HasValue && promo.ExpiryDate.Value < DateTime.UtcNow) return new(false, "Code expired", promo, 0m, false);
            if (promo.MinPurchase.HasValue && subtotal < promo.MinPurchase.Value) return new(false, $"Minimum purchase {promo.MinPurchase.Value:C} not met", promo, 0m, false);
            if (promo.RemainingUses.HasValue && promo.RemainingUses.Value <= 0) return new(false, "Usage limit exceeded", promo, 0m, false);
            if (promo.PerUserLimit.HasValue)
            {
                var usedCount = await _db.PromoCodeUsages.CountAsync(u => u.PromoCodeId == promo.Id && u.UserId == userId);
                if (usedCount >= promo.PerUserLimit.Value) return new(false, "Per-user usage limit reached", promo, 0m, false);
            }
            var (discountAmount, freeShip) = ComputeDiscount(promo, subtotal);
            return new(true, "Valid", promo, discountAmount, freeShip);
        }

        public async Task<CouponValidationResult> ApplyAsync(string code, int userId, decimal subtotal)
        {
            // Validate then record usage (atomic)
            var validation = await ValidateAsync(code, userId, subtotal);
            if (!validation.IsValid || validation.Promo == null) return validation;
            // Record usage inside a transaction reducing RemainingUses (if limited)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var promo = await _db.PromoCodes.FirstAsync(p => p.Id == validation.Promo.Id);
                if (promo.RemainingUses.HasValue && promo.RemainingUses.Value <= 0)
                {
                    await tx.RollbackAsync();
                    return validation with { IsValid = false, Message = "Usage limit exceeded" };
                }
                if (promo.PerUserLimit.HasValue)
                {
                    var count = await _db.PromoCodeUsages.CountAsync(u => u.PromoCodeId == promo.Id && u.UserId == userId);
                    if (count >= promo.PerUserLimit.Value)
                    {
                        await tx.RollbackAsync();
                        return validation with { IsValid = false, Message = "Per-user usage limit reached" };
                    }
                }
                _db.PromoCodeUsages.Add(new PromoCodeUsage { PromoCodeId = promo.Id, UserId = userId });
                if (promo.RemainingUses.HasValue)
                {
                    promo.RemainingUses = Math.Max(0, promo.RemainingUses.Value - 1);
                    _db.PromoCodes.Update(promo);
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return validation;
            }
            catch
            {
                await tx.RollbackAsync();
                return validation with { IsValid = false, Message = "Failed to apply coupon" };
            }
        }

        public async Task<bool> RecordUsageAsync(string code, int userId)
        {
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Code == code);
            if (promo == null) return false;
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.PromoCodeUsages.Add(new PromoCodeUsage { PromoCodeId = promo.Id, UserId = userId });
                if (promo.RemainingUses.HasValue)
                {
                    if (promo.RemainingUses.Value <= 0) { await tx.RollbackAsync(); return false; }
                    promo.RemainingUses = Math.Max(0, promo.RemainingUses.Value - 1);
                    _db.PromoCodes.Update(promo);
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                return false;
            }
        }

        private static (decimal discountAmount, bool freeShipping) ComputeDiscount(PromoCode promo, decimal subtotal)
        {
            if (string.Equals(promo.Type, "FreeShipping", StringComparison.OrdinalIgnoreCase))
                return (0m, true);
            if (string.Equals(promo.Type, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                var amt = promo.FixedAmount ?? 0m;
                return (Math.Min(subtotal, amt), false);
            }
            // percentage default
            var pct = promo.DiscountPercent;
            var d = Math.Round(subtotal * pct / 100m, 2);
            return (d, false);
        }
    }
}
