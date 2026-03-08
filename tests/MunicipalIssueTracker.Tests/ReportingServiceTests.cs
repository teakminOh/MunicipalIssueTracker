using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Web.Services;

namespace MunicipalIssueTracker.Tests;

public class ReportingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ReportingService _svc;

    public ReportingServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        SeedTestData();
        _svc = new ReportingService(_db);
    }

    private void SeedTestData()
    {
        var district1 = new District { DistrictId = 1, Name = "Centrum" };
        var district2 = new District { DistrictId = 2, Name = "Södermalm" };
        var category1 = new Category { CategoryId = 1, Name = "Pothole" };
        var category2 = new Category { CategoryId = 2, Name = "Graffiti" };
        var statusOpen = new Status { StatusId = 1, Name = "Open", SortOrder = 1 };
        var statusResolved = new Status { StatusId = 2, Name = "Resolved", SortOrder = 4 };
        var user = new User { UserId = 1, Email = "admin@test.se", PasswordHash = "x", DisplayName = "Admin", Role = UserRole.Admin };

        _db.Districts.AddRange(district1, district2);
        _db.Categories.AddRange(category1, category2);
        _db.Statuses.AddRange(statusOpen, statusResolved);
        _db.Users.Add(user);
        _db.SaveChanges();

        _db.Issues.AddRange(
            new Issue { IssueId = 1, Title = "Hole A", Description = "Desc", DistrictId = 1, CategoryId = 1, StatusId = 1, Priority = IssuePriority.High, Lat = 59.33, Lng = 18.07, AddressText = "Addr", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new Issue { IssueId = 2, Title = "Hole B", Description = "Desc", DistrictId = 1, CategoryId = 1, StatusId = 2, Priority = IssuePriority.Low, Lat = 59.33, Lng = 18.07, AddressText = "Addr", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow },
            new Issue { IssueId = 3, Title = "Tag C", Description = "Desc", DistrictId = 2, CategoryId = 2, StatusId = 1, Priority = IssuePriority.Critical, Lat = 59.32, Lng = 18.08, AddressText = "Addr", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow }
        );
        _db.SaveChanges();

        _db.Comments.AddRange(
            new Comment { CommentId = 1, IssueId = 1, AuthorUserId = 1, Body = "First comment", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Comment { CommentId = 2, IssueId = 1, AuthorUserId = 1, Body = "Latest comment on issue 1", CreatedAt = DateTime.UtcNow },
            new Comment { CommentId = 3, IssueId = 3, AuthorUserId = 1, Body = "Only comment on issue 3", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetIssueCountsByDistrictAndStatus_ReturnsGroupedCounts()
    {
        var results = await _svc.GetIssueCountsByDistrictAndStatusAsync();

        Assert.NotEmpty(results);
        var centrumOpen = results.FirstOrDefault(r => r.DistrictName == "Centrum" && r.StatusName == "Open");
        Assert.NotNull(centrumOpen);
        Assert.Equal(1, centrumOpen.IssueCount);

        var centrumResolved = results.FirstOrDefault(r => r.DistrictName == "Centrum" && r.StatusName == "Resolved");
        Assert.NotNull(centrumResolved);
        Assert.Equal(1, centrumResolved.IssueCount);
    }

    [Fact]
    public async Task GetIssueCountsByCategoryAndPriority_ReturnsGroupedCounts()
    {
        var results = await _svc.GetIssueCountsByCategoryAndPriorityAsync();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.CategoryName == "Pothole");
        Assert.Contains(results, r => r.CategoryName == "Graffiti");
    }

    [Fact]
    public async Task GetLatestCommentPerIssue_ReturnsLatestOnly()
    {
        var results = await _svc.GetLatestCommentPerIssueAsync();

        // Issue 1 has 2 comments — should return only the latest
        var issue1 = results.FirstOrDefault(r => r.IssueId == 1);
        Assert.NotNull(issue1);
        Assert.Equal("Latest comment on issue 1", issue1.LatestComment);

        // Issue 2 has no comments — should not appear
        Assert.DoesNotContain(results, r => r.IssueId == 2);

        // Issue 3 has 1 comment
        var issue3 = results.FirstOrDefault(r => r.IssueId == 3);
        Assert.NotNull(issue3);
        Assert.Equal("Only comment on issue 3", issue3.LatestComment);
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsCorrectCounts()
    {
        var summary = await _svc.GetDashboardSummaryAsync();

        Assert.Equal(3, summary.TotalIssues);
        Assert.Equal(2, summary.OpenIssues);       // StatusId 1 (SortOrder=1) — issues 1 and 3
        Assert.Equal(1, summary.ResolvedIssues);    // StatusId 2 (SortOrder=4) — issue 2
        Assert.Equal(1, summary.CriticalOpenIssues); // Issue 3 (Critical + Open)
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
