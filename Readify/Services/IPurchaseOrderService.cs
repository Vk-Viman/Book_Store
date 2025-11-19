using Readify.Models;

namespace Readify.Services
{
    public interface IPurchaseOrderService
    {
        Task<PurchaseOrder> CreateAsync(int supplierId, IEnumerable<(int ProductId, int Quantity, decimal UnitPrice)> items, CancellationToken ct = default);
        Task<PurchaseOrder?> ReceiveAsync(int purchaseOrderId, int? receivedByUserId = null, CancellationToken ct = default);
        Task<PurchaseOrder?> ReceivePartialAsync(int purchaseOrderId, IEnumerable<(int PurchaseOrderItemId, int ReceivedQuantity)> items, int? receivedByUserId = null, CancellationToken ct = default);
        Task<List<PurchaseOrder>> ListAsync(CancellationToken ct = default);
        Task<PurchaseOrder?> GetAsync(int id, CancellationToken ct = default);
        Task CancelAsync(int id, CancellationToken ct = default);
    }
}
