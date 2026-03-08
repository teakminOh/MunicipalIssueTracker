namespace MunicipalIssueTracker.Domain.Entities;

public class Attachment
{
    public int AttachmentId { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
