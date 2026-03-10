using System.ComponentModel.DataAnnotations;

namespace MunicipalIssueTracker.Domain.Entities;

public class AuditLog
{
    public int AuditId { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public int ActorUserId { get; set; }
    public User Actor { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(4000)]
    public string? DetailsJson { get; set; }
}
