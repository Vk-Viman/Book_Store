using Readify.Services;
using System.Threading.Tasks;
using System.Threading;

namespace Readify.Services
{
    public class NullAuditService : IAuditService
    {
        public Task WriteAsync(string action, string entity, int? entityId, string? details = null, CancellationToken ct = default) => Task.CompletedTask;
    }
}
