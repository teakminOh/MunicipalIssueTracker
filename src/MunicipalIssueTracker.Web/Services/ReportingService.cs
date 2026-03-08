using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Web.Services;

public class ReportingService
{
    private readonly AppDbContext _db;

    public ReportingService(AppDbContext db) => _db = db;

    /// <summary>
    /// Issue counts grouped by district and status.
    /// Demonstrates: JOIN + GROUP BY + aggregate SQL thinking.
    /// Uses raw SQL via FromSqlRaw to explicitly show SQL competency.
    /// </summary>
    public async Task<List<DistrictStatusCount>> GetIssueCountsByDistrictAndStatusAsync()
    {
        // Explicit SQL to demonstrate JOIN + GROUP BY knowledge
        var sql = @"
            SELECT d.Name AS DistrictName, s.Name AS StatusName, COUNT(*) AS IssueCount
            FROM Issues i
            INNER JOIN Districts d ON i.DistrictId = d.DistrictId
            INNER JOIN Statuses s ON i.StatusId = s.StatusId
            GROUP BY d.Name, s.Name
            ORDER BY d.Name, s.Name";

        using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync();

        var results = new List<DistrictStatusCount>();
        while (await reader.ReadAsync())
        {
            results.Add(new DistrictStatusCount
            {
                DistrictName = reader.GetString(0),
                StatusName = reader.GetString(1),
                IssueCount = reader.GetInt32(2)
            });
        }
        return results;
    }

    /// <summary>
    /// Issues by category with priority breakdown.
    /// Demonstrates: GROUP BY with multiple dimensions.
    /// </summary>
    public async Task<List<CategoryPriorityCount>> GetIssueCountsByCategoryAndPriorityAsync()
    {
        var sql = @"
            SELECT c.Name AS CategoryName, i.Priority, COUNT(*) AS IssueCount
            FROM Issues i
            INNER JOIN Categories c ON i.CategoryId = c.CategoryId
            GROUP BY c.Name, i.Priority
            ORDER BY c.Name, i.Priority";

        using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync();

        var results = new List<CategoryPriorityCount>();
        while (await reader.ReadAsync())
        {
            results.Add(new CategoryPriorityCount
            {
                CategoryName = reader.GetString(0),
                Priority = reader.GetString(1),
                IssueCount = reader.GetInt32(2)
            });
        }
        return results;
    }

    /// <summary>
    /// Latest comment per issue — demonstrates subquery / window function thinking.
    /// </summary>
    public async Task<List<LatestCommentReport>> GetLatestCommentPerIssueAsync()
    {
        var sql = @"
            SELECT i.IssueId, i.Title, c.Body AS LatestComment, u.DisplayName AS AuthorName, c.CreatedAt
            FROM Issues i
            INNER JOIN Comments c ON c.CommentId = (
                SELECT c2.CommentId FROM Comments c2
                WHERE c2.IssueId = i.IssueId
                ORDER BY c2.CreatedAt DESC
                LIMIT 1
            )
            INNER JOIN Users u ON c.AuthorUserId = u.UserId
            ORDER BY c.CreatedAt DESC";

        using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync();

        var results = new List<LatestCommentReport>();
        while (await reader.ReadAsync())
        {
            results.Add(new LatestCommentReport
            {
                IssueId = reader.GetInt32(0),
                IssueTitle = reader.GetString(1),
                LatestComment = reader.GetString(2),
                AuthorName = reader.GetString(3),
                CommentedAt = reader.GetDateTime(4)
            });
        }
        return results;
    }

    /// <summary>
    /// Summary statistics for the dashboard.
    /// </summary>
    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        var totalIssues = await _db.Issues.CountAsync();
        var openIssues = await _db.Issues.CountAsync(i => i.Status.SortOrder < 4);
        var resolvedIssues = await _db.Issues.CountAsync(i => i.Status.SortOrder >= 4);
        var criticalIssues = await _db.Issues.CountAsync(i => i.Priority == Domain.Enums.IssuePriority.Critical && i.Status.SortOrder < 4);

        return new DashboardSummary
        {
            TotalIssues = totalIssues,
            OpenIssues = openIssues,
            ResolvedIssues = resolvedIssues,
            CriticalOpenIssues = criticalIssues
        };
    }
}

// Report DTOs
public class DistrictStatusCount
{
    public string DistrictName { get; set; } = "";
    public string StatusName { get; set; } = "";
    public int IssueCount { get; set; }
}

public class CategoryPriorityCount
{
    public string CategoryName { get; set; } = "";
    public string Priority { get; set; } = "";
    public int IssueCount { get; set; }
}

public class LatestCommentReport
{
    public int IssueId { get; set; }
    public string IssueTitle { get; set; } = "";
    public string LatestComment { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public DateTime CommentedAt { get; set; }
}

public class DashboardSummary
{
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int ResolvedIssues { get; set; }
    public int CriticalOpenIssues { get; set; }
}
