using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Web.Services;

public class ReportingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(AppDbContext db, ILogger<ReportingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Issue counts grouped by district and status.
    /// Uses CROSS JOIN to produce all district×status combinations (including 0-count),
    /// then LEFT JOIN to Issues to count actual issues per combination.
    /// </summary>
    public async Task<List<DistrictStatusCount>> GetIssueCountsByDistrictAndStatusAsync()
    {
        var sql = @"
            SELECT d.Name AS DistrictName, s.Name AS StatusName,
                   COUNT(i.IssueId) AS IssueCount, s.SortOrder AS StatusSortOrder
            FROM Districts d
            CROSS JOIN Statuses s
            LEFT JOIN Issues i ON i.DistrictId = d.DistrictId AND i.StatusId = s.StatusId
            GROUP BY d.Name, s.Name, s.SortOrder
            ORDER BY d.Name, s.SortOrder";

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
                IssueCount = reader.GetInt32(2),
                StatusSortOrder = reader.GetInt32(3)
            });
        }
        return results;
    }

    /// <summary>
    /// Issues by category with priority breakdown.
    /// Uses CROSS JOIN to produce all category×priority combinations (including 0-count).
    /// Priority enum values are enumerated via a CTE.
    /// </summary>
    public async Task<List<CategoryPriorityCount>> GetIssueCountsByCategoryAndPriorityAsync()
    {
        var sql = @"
            WITH Priorities(Priority, PriorityOrder) AS (
                SELECT 'Low', 0 UNION ALL
                SELECT 'Medium', 1 UNION ALL
                SELECT 'High', 2 UNION ALL
                SELECT 'Critical', 3
            )
            SELECT c.Name AS CategoryName, p.Priority, COUNT(i.IssueId) AS IssueCount, p.PriorityOrder
            FROM Categories c
            CROSS JOIN Priorities p
            LEFT JOIN Issues i ON i.CategoryId = c.CategoryId AND i.Priority = p.Priority
            GROUP BY c.Name, p.Priority, p.PriorityOrder
            ORDER BY c.Name, p.PriorityOrder";

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
                IssueCount = reader.GetInt32(2),
                PriorityOrder = reader.GetInt32(3)
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

    /// <summary>
    /// Open (non-terminal) issue counts per district.
    /// LEFT JOIN ensures districts with 0 open issues still appear.
    /// </summary>
    public async Task<List<DistrictIssueCount>> GetOpenIssueCountsByDistrictAsync()
    {
        var sql = @"
            SELECT d.Name AS DistrictName, COUNT(i.IssueId) AS IssueCount
            FROM Districts d
            LEFT JOIN Issues i ON i.DistrictId = d.DistrictId
                AND i.StatusId IN (SELECT s.StatusId FROM Statuses s WHERE s.IsTerminal = 0)
            GROUP BY d.Name
            ORDER BY COUNT(i.IssueId) DESC";

        using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync();

        var results = new List<DistrictIssueCount>();
        while (await reader.ReadAsync())
        {
            results.Add(new DistrictIssueCount
            {
                DistrictName = reader.GetString(0),
                IssueCount = reader.GetInt32(1)
            });
        }
        return results;
    }

    /// <summary>
    /// Total issue counts per category, sorted descending.
    /// LEFT JOIN ensures categories with 0 issues still appear.
    /// </summary>
    public async Task<List<CategoryIssueCount>> GetIssueCountsByCategoryAsync()
    {
        var sql = @"
            SELECT c.Name AS CategoryName, COUNT(i.IssueId) AS IssueCount
            FROM Categories c
            LEFT JOIN Issues i ON i.CategoryId = c.CategoryId
            GROUP BY c.Name
            ORDER BY COUNT(i.IssueId) DESC";

        using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync();

        var results = new List<CategoryIssueCount>();
        while (await reader.ReadAsync())
        {
            results.Add(new CategoryIssueCount
            {
                CategoryName = reader.GetString(0),
                IssueCount = reader.GetInt32(1)
            });
        }
        return results;
    }
}

// Report DTOs
public class DistrictStatusCount
{
    public string DistrictName { get; set; } = "";
    public string StatusName { get; set; } = "";
    public int IssueCount { get; set; }
    public int StatusSortOrder { get; set; }
}

public class CategoryPriorityCount
{
    public string CategoryName { get; set; } = "";
    public string Priority { get; set; } = "";
    public int IssueCount { get; set; }
    public int PriorityOrder { get; set; }
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

public class DistrictIssueCount
{
    public string DistrictName { get; set; } = "";
    public int IssueCount { get; set; }
}

public class CategoryIssueCount
{
    public string CategoryName { get; set; } = "";
    public int IssueCount { get; set; }
}
