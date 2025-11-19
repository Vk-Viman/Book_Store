using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Readify.Data;
using Microsoft.EntityFrameworkCore;

namespace Readify.Services
{
    public class StockMonitoringService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<StockMonitoringService> _logger;
        private readonly IConfiguration _config;
        private readonly TimeSpan _interval;

        public StockMonitoringService(IServiceProvider services, ILogger<StockMonitoringService> logger, IConfiguration config)
        {
            _services = services;
            _logger = logger;
            _config = config;
            var minutes = _config.GetValue<int?>("StockMonitoring:IntervalMinutes") ?? 5;
            _interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StockMonitoringService started, interval={Interval}", _interval);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var poSvc = scope.ServiceProvider.GetService<IPurchaseOrderService>();

                    // load products with initial stock > 0 and where stock <= 10% of initial (or <= 1)
                    // Use integer arithmetic to compute ceil(initial/10) as (InitialStock + 9) / 10 so EF can translate to SQL
                    var prods = await db.Products
                        .Where(p => p.InitialStock > 0 && p.StockQty <= ((p.InitialStock + 9) / 10))
                        .ToListAsync(stoppingToken);

                    if (prods.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} low-stock products", prods.Count);
                        // group by supplier if supplier exists on product? For simplicity, use configured default supplier
                        var defaultSupplierId = _config.GetValue<int?>("StockMonitoring:DefaultSupplierId");
                        var autoCreate = _config.GetValue<bool?>("StockMonitoring:AutoCreatePurchaseOrders") ?? false;

                        if (autoCreate && defaultSupplierId.HasValue && poSvc != null)
                        {
                            foreach (var p in prods)
                            {
                                try
                                {
                                    var needed = Math.Max(p.InitialStock, 10) - p.StockQty;
                                    if (needed <= 0) continue;
                                    // check if there is already a pending PO for this product
                                    var exists = await db.PurchaseOrderItems
                                        .Where(i => i.ProductId == p.Id)
                                        .Join(db.PurchaseOrders.Where(po => po.Status == "Pending"),
                                            item => item.PurchaseOrderId,
                                            po => po.Id,
                                            (item, po) => new { item.ProductId })
                                        .AnyAsync(stoppingToken);
                                    if (exists) continue;

                                    var po = await poSvc.CreateAsync(defaultSupplierId.Value, new[] { (p.Id, needed, p.Price) }, stoppingToken);
                                    _logger.LogInformation("Auto-created PO {PoId} for product {ProductId} qty {Qty}", po.Id, p.Id, needed);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to auto-create PO for product {ProductId}", p.Id);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Auto-create disabled or no default supplier configured; low-stock items: {Items}", string.Join(',', prods.Select(p => p.Id)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stock monitoring iteration failed");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException) { break; }
            }
            _logger.LogInformation("StockMonitoringService stopping");
        }
    }
}
