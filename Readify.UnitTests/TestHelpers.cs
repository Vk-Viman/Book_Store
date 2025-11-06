using System.Threading.Tasks;
using Readify.Services;

namespace Readify.UnitTests
{
    public class MockShippingService : IShippingService
    {
        public Task<decimal> GetRateAsync(string region, decimal subtotal) => Task.FromResult(1m);
    }

    public class FailingPaymentService : IPaymentService
    {
        public Task<(bool Success, string? TransactionId)> ChargeAsync(decimal amount, string? method = null, string? token = null) => Task.FromResult<(bool, string?)>((false, null));
        public Task<bool> RefundAsync(decimal amount, string? transactionId = null) => Task.FromResult(false);
    }
}
