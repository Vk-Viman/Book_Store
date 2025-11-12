using Readify.Services;
using System.Threading.Tasks;

namespace Readify.Services
{
    public class NullAuditService : IAuditService
    {
        public Task WriteAsync(string action, string entity, int entityId, string? details = null) => Task.CompletedTask;
    }
}
