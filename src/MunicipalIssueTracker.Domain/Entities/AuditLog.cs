namespace MunicipalIssueTracker.Domain.Entities;

public class AuditLog
{
    public int AuditId { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public int ActorUserId { get; set; }
    public User Actor { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? DetailsJson { get; set; }
}
