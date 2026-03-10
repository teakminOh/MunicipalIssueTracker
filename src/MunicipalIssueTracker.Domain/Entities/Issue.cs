using System.ComponentModel.DataAnnotations;
using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Domain.Entities;

public class Issue
{
    public int IssueId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Status is required.")]
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "District is required.")]
    public int DistrictId { get; set; }
    public District District { get; set; } = null!;

    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Lat { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Lng { get; set; }

    [StringLength(300)]
    public string AddressText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;

    public int? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }

    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    [StringLength(20)]
    public string TrackingCode { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? ResolutionNote { get; set; }

    // Navigation properties
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
