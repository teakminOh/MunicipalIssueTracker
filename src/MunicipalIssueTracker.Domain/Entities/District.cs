using System.ComponentModel.DataAnnotations;

namespace MunicipalIssueTracker.Domain.Entities;

public class District
{
    public int DistrictId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
