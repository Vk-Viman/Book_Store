using System.Threading;
using System.Threading.Tasks;

namespace Readify.Services
{
    public interface IAuditService
    {
        Task WriteAsync(string action, string entity, int? entityId, string? details = null, CancellationToken ct = default);
    }
}