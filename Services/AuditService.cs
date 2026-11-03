using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string? recordId = null, string? oldValue = null, string? newValue = null, string? userId = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    public AuditService(ApplicationDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public async Task LogAsync(string action, string entity, string? recordId = null, string? oldValue = null, string? newValue = null, string? userId = null)
    {
        var ctx = _http.HttpContext;
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            EntityName = entity,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}
