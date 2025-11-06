using Microsoft.Extensions.Options;
using Readify.Models;
using System.Threading.Tasks;
using Readify.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Readify.Services
{
    public class ShippingOptions
    {
        public decimal Local { get; set; } = 2.00m;
        public decimal National { get; set; } = 5.00m;
        public decimal International { get; set; } = 15.00m;
        public decimal FreeShippingThreshold { get; set; } = 100.00m;
    }

    public class ShippingService : IShippingService
    {
        private readonly ShippingOptions _opts;
        private readonly IServiceScopeFactory _scopeFactory;

        public ShippingService(IOptions<ShippingOptions> opts, IServiceScopeFactory scopeFactory)
        {
            _opts = opts?.Value ?? new ShippingOptions();
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<decimal> GetRateAsync(string? region, decimal subtotal)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
                if (db != null)
                {
                    var setting = await db.ShippingSettings.OrderByDescending(s => s.UpdatedAt).FirstOrDefaultAsync();
                    if (setting != null)
                    {
                        var rKey = (region ?? "national").ToLowerInvariant();
                        decimal rate = rKey switch
                        {
                            "local" => setting.Local,
                            "national" => setting.National,
                            "international" => setting.International,
                            _ => setting.National
                        };
                        if (subtotal >= setting.FreeShippingThreshold) rate = 0m;
                        return rate;
                    }
                }
            }
            catch
            {
                // ignore DB errors and fall back to configured options
            }

            var r = (region ?? "national").ToLowerInvariant();
            decimal cfgRate = r switch
            {
                "local" => _opts.Local,
                "national" => _opts.National,
                "international" => _opts.International,
                _ => _opts.National
            };

            if (subtotal >= _opts.FreeShippingThreshold) cfgRate = 0m;

            return cfgRate;
        }
    }
}
