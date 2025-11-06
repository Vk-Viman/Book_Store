using System.Threading.Tasks;

namespace Readify.Services;

public interface IPaymentService
{
    Task<(bool Success, string? TransactionId)> ChargeAsync(decimal amount, string? method = null, string? token = null);
    Task<bool> RefundAsync(decimal amount, string? transactionId = null);
}
