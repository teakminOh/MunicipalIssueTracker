using System.ComponentModel.DataAnnotations;

namespace MunicipalIssueTracker.Domain.Entities;

public class Status
{
    public int StatusId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsTerminal { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
