using System.ComponentModel.DataAnnotations;
using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Domain.Entities;

public class User
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(200)]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    [Required]
    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Issue> CreatedIssues { get; set; } = new List<Issue>();
    public ICollection<Issue> AssignedIssues { get; set; } = new List<Issue>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
