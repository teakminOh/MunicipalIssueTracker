namespace MunicipalIssueTracker.Domain.Entities;

public class Status
{
    public int StatusId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
