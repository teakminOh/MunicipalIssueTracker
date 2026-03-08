using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Web.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(int issueId, int actorUserId, string action, string? detailsJson = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            IssueId = issueId,
            ActorUserId = actorUserId,
            Action = action,
            DetailsJson = detailsJson,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
