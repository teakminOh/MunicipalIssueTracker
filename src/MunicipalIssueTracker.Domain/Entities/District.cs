namespace MunicipalIssueTracker.Domain.Entities;

public class District
{
    public int DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
