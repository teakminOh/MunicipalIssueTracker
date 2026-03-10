using System.ComponentModel.DataAnnotations;

namespace MunicipalIssueTracker.Domain.Entities;

public class Attachment
{
    public int AttachmentId { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, 5 * 1024 * 1024, ErrorMessage = "File size must be between 1 byte and 5 MB.")]
    public long SizeBytes { get; set; }

    [Required]
    [StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
