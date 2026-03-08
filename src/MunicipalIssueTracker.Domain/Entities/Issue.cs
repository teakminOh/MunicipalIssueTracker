using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Domain.Entities;

public class Issue
{
    public int IssueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public int DistrictId { get; set; }
    public District District { get; set; } = null!;

    public double Lat { get; set; }
    public double Lng { get; set; }
    public string AddressText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;

    public int? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }

    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    // Navigation properties
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
