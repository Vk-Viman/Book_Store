using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Readify.Services;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;
    private readonly ConcurrentDictionary<string, decimal> _charges = new();
    private readonly Random _rng = new();

    public MockPaymentService(ILogger<MockPaymentService> logger) => _logger = logger;

    public Task<bool> ChargeAsync(decimal amount, string? method = null, string? token = null)
    {
        // simulate random failures for testing; deterministic if token provided
        var key = token ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Mock charge: amount={Amount} token={Token}", amount, token);

        if (amount <= 0) return Task.FromResult(false);

        var fail = false;
        if (!string.IsNullOrEmpty(token))
        {
            // if token contains "fail" cause failure; if contains "flaky" fail 50%
            if (token.Contains("fail")) fail = true;
            else if (token.Contains("flaky") && _rng.NextDouble() < 0.5) fail = true;
        }
        else
        {
            // small chance of random failure
            if (_rng.NextDouble() < 0.02) fail = true;
        }

        if (fail)
        {
            _logger.LogWarning("Mock payment failed for token={Token}", token);
            return Task.FromResult(false);
        }

        _charges[key] = amount;
        return Task.FromResult(true);
    }

    public Task<bool> RefundAsync(decimal amount, string? transactionId = null)
    {
        // simulate refund success if transaction id exists and amount <= charged
        if (string.IsNullOrEmpty(transactionId)) return Task.FromResult(false);
        if (!_charges.TryGetValue(transactionId, out var charged)) return Task.FromResult(false);
        if (amount > charged) return Task.FromResult(false);
        _logger.LogInformation("Mock refund: transactionId={Tid} amount={Amount}", transactionId, amount);
        _charges.TryRemove(transactionId, out _);
        return Task.FromResult(true);
    }
}
