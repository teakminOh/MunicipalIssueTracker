namespace MunicipalIssueTracker.Domain.Entities;

public class Comment
{
    public int CommentId { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public int AuthorUserId { get; set; }
    public User Author { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
