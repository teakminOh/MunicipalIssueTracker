using System.ComponentModel.DataAnnotations;
using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Icon { get; set; } = "report_problem";

    public IssuePriority DefaultPriority { get; set; } = IssuePriority.Medium;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
