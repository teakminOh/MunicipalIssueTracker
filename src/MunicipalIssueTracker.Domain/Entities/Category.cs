using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "report_problem";
    public IssuePriority DefaultPriority { get; set; } = IssuePriority.Medium;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
