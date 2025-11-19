using Microsoft.AspNetCore.Http;
using Readify.Data;
using Readify.Models;

namespace Readify.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;
        public AuditService(AppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task WriteAsync(string action, string entity, int? entityId, string? details = null, CancellationToken ct = default)
        {
            int? userId = null;
            var uid = _http.HttpContext?.User?.FindFirst("userId")?.Value;
            if (int.TryParse(uid, out var parsed)) userId = parsed;
            var log = new AuditLog
            {
                Action = action,
                Entity = entity,
                EntityId = entityId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Details = details
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
    }
}
