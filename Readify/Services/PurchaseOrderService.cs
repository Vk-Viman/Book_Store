using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PurchaseOrderService> _logger;

        public PurchaseOrderService(AppDbContext db, ILogger<PurchaseOrderService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<PurchaseOrder> CreateAsync(int supplierId, IEnumerable<(int ProductId, int Quantity, decimal UnitPrice)> items, CancellationToken ct = default)
        {
            var sup = await _db.Suppliers.FindAsync(supplierId);
            if (sup == null) throw new InvalidOperationException("Supplier not found");

            var po = new PurchaseOrder { SupplierId = supplierId, OrderDate = DateTime.UtcNow, Status = "Pending", CreatedAt = DateTime.UtcNow };
            decimal total = 0m;
            foreach (var it in items)
            {
                if (it.ProductId <= 0 || it.Quantity <= 0) continue;
                var prod = await _db.Products.FindAsync(it.ProductId);
                decimal unit = it.UnitPrice >= 0 ? it.UnitPrice : (prod?.Price ?? 0m);
                po.Items.Add(new PurchaseOrderItem { ProductId = it.ProductId, Quantity = it.Quantity, UnitPrice = unit });
                total += unit * it.Quantity;
            }
            po.TotalAmount = total;
            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync(ct);
            return po;
        }

        public async Task<PurchaseOrder?> ReceiveAsync(int purchaseOrderId, int? receivedByUserId = null, CancellationToken ct = default)
        {
            var po = await _db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct);
            if (po == null) return null;
            if (po.Status == "Received") return po;
            if (po.Status == "Cancelled") throw new InvalidOperationException("Cannot receive a cancelled PO");

            foreach (var it in po.Items)
            {
                var prod = await _db.Products.FindAsync(it.ProductId);
                if (prod == null) continue;
                prod.StockQty += it.Quantity;
                if (prod.InitialStock <= 0 || prod.InitialStock < prod.StockQty) prod.InitialStock = prod.StockQty;
                it.ReceivedQuantity = it.Quantity;
            }

            po.Status = "Received";
            po.ReceivedAt = DateTime.UtcNow;
            po.ReceivedByUserId = receivedByUserId;
            po.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return po;
        }

        public async Task<PurchaseOrder?> ReceivePartialAsync(int purchaseOrderId, IEnumerable<(int PurchaseOrderItemId, int ReceivedQuantity)> items, int? receivedByUserId = null, CancellationToken ct = default)
        {
            var po = await _db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct);
            if (po == null) return null;
            if (po.Status == "Cancelled") throw new InvalidOperationException("Cannot receive a cancelled PO");

            foreach (var it in items)
            {
                var poi = po.Items.FirstOrDefault(x => x.Id == it.PurchaseOrderItemId);
                if (poi == null) continue;
                var toReceive = Math.Max(0, Math.Min(it.ReceivedQuantity, poi.Quantity - poi.ReceivedQuantity));
                if (toReceive <= 0) continue;
                var prod = await _db.Products.FindAsync(poi.ProductId);
                if (prod != null)
                {
                    prod.StockQty += toReceive;
                    if (prod.InitialStock <= 0 || prod.InitialStock < prod.StockQty) prod.InitialStock = prod.StockQty;
                }
                poi.ReceivedQuantity += toReceive;
            }

            // if all items fully received mark PO as Received
            if (po.Items.All(x => x.ReceivedQuantity >= x.Quantity))
            {
                po.Status = "Received";
                po.ReceivedAt = DateTime.UtcNow;
                po.ReceivedByUserId = receivedByUserId;
            }

            po.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return po;
        }

        public async Task<List<PurchaseOrder>> ListAsync(CancellationToken ct = default)
        {
            return await _db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .ToListAsync(ct);
        }

        public async Task<PurchaseOrder?> GetAsync(int id, CancellationToken ct = default)
        {
            return await _db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task CancelAsync(int id, CancellationToken ct = default)
        {
            var po = await _db.PurchaseOrders.FindAsync(new object[] { id }, ct);
            if (po == null) throw new KeyNotFoundException("Purchase order not found");
            if (po.Status == "Received") throw new InvalidOperationException("Cannot cancel a received PO");
            po.Status = "Cancelled";
            po.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}
