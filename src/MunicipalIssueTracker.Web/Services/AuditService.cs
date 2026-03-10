using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Web.Services;

public class AuditService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

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
        _logger.LogInformation("Audit: {Action} on issue {IssueId} by user {ActorUserId}", action, issueId, actorUserId);
    }
}
