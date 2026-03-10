using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
        _svc = new ReportingService(_db, NullLogger<ReportingService>.Instance);
    }

    private void SeedTestData()
    {
        var district1 = new District { DistrictId = 1, Name = "Centrum" };
        var district2 = new District { DistrictId = 2, Name = "Sídlisko Brehy" };
        var category1 = new Category { CategoryId = 1, Name = "Pothole" };
        var category2 = new Category { CategoryId = 2, Name = "Graffiti" };
        var statusOpen = new Status { StatusId = 1, Name = "Open", SortOrder = 1, IsTerminal = false };
        var statusResolved = new Status { StatusId = 2, Name = "Resolved", SortOrder = 4, IsTerminal = true };
        var user = new User { UserId = 1, Email = "admin@namestovo.sk", PasswordHash = "x", DisplayName = "Admin", Role = UserRole.Admin };

        _db.Districts.AddRange(district1, district2);
        _db.Categories.AddRange(category1, category2);
        _db.Statuses.AddRange(statusOpen, statusResolved);
        _db.Users.Add(user);
        _db.SaveChanges();

        _db.Issues.AddRange(
            new Issue { IssueId = 1, Title = "Hole A", Description = "Desc", DistrictId = 1, CategoryId = 1, StatusId = 1, Priority = IssuePriority.High, Lat = 49.41, Lng = 19.48, AddressText = "Addr", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow, TrackingCode = "NAM-2026-0001" },
            new Issue { IssueId = 2, Title = "Hole B", Description = "Desc", DistrictId = 1, CategoryId = 1, StatusId = 2, Priority = IssuePriority.Low, Lat = 49.41, Lng = 19.48, AddressText = "Addr", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow, TrackingCode = "NAM-2026-0002" },
            new Issue { IssueId = 3, Title = "Tag C", Description = "Desc", DistrictId = 2, CategoryId = 2, StatusId = 1, Priority = IssuePriority.Critical, Lat = 49.40, Lng = 19.49, AddressText = "Addr", CreatedByUserId = 1, CreatedAt = DateTime.UtcNow, TrackingCode = "NAM-2026-0003" }
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
    public async Task GetIssueCountsByDistrictAndStatus_IncludesZeroCountCombinations()
    {
        var results = await _svc.GetIssueCountsByDistrictAndStatusAsync();

        // CROSS JOIN produces all district×status combos: 2 districts × 2 statuses = 4 rows
        Assert.Equal(4, results.Count);

        // Sídlisko Brehy has no Resolved issues — should still appear with count 0
        var brehyResolved = results.FirstOrDefault(r => r.DistrictName == "Sídlisko Brehy" && r.StatusName == "Resolved");
        Assert.NotNull(brehyResolved);
        Assert.Equal(0, brehyResolved.IssueCount);
    }

    [Fact]
    public async Task GetIssueCountsByDistrictAndStatus_IncludesStatusSortOrder()
    {
        var results = await _svc.GetIssueCountsByDistrictAndStatusAsync();

        var openRow = results.First(r => r.StatusName == "Open");
        var resolvedRow = results.First(r => r.StatusName == "Resolved");
        Assert.True(openRow.StatusSortOrder < resolvedRow.StatusSortOrder);
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
    public async Task GetIssueCountsByCategoryAndPriority_IncludesZeroCountCombinations()
    {
        var results = await _svc.GetIssueCountsByCategoryAndPriorityAsync();

        // 2 categories × 4 priorities = 8 rows
        Assert.Equal(8, results.Count);

        // Graffiti has no Low-priority issues — should still appear with count 0
        var graffitiLow = results.FirstOrDefault(r => r.CategoryName == "Graffiti" && r.Priority == "Low");
        Assert.NotNull(graffitiLow);
        Assert.Equal(0, graffitiLow.IssueCount);
    }

    [Fact]
    public async Task GetIssueCountsByCategoryAndPriority_IncludesPriorityOrder()
    {
        var results = await _svc.GetIssueCountsByCategoryAndPriorityAsync();

        var lowRow = results.First(r => r.Priority == "Low");
        var criticalRow = results.First(r => r.Priority == "Critical");
        Assert.True(lowRow.PriorityOrder < criticalRow.PriorityOrder);
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

    [Fact]
    public async Task GetOpenIssueCountsByDistrict_ReturnsOnlyNonTerminal()
    {
        var results = await _svc.GetOpenIssueCountsByDistrictAsync();

        // Both districts should appear
        Assert.Equal(2, results.Count);

        // Centrum: issue 1 is Open (non-terminal), issue 2 is Resolved (terminal) → 1 open
        var centrum = results.First(r => r.DistrictName == "Centrum");
        Assert.Equal(1, centrum.IssueCount);

        // Sídlisko Brehy: issue 3 is Open (non-terminal) → 1 open
        var brehy = results.First(r => r.DistrictName == "Sídlisko Brehy");
        Assert.Equal(1, brehy.IssueCount);
    }

    [Fact]
    public async Task GetOpenIssueCountsByDistrict_SortedDescending()
    {
        var results = await _svc.GetOpenIssueCountsByDistrictAsync();

        // Both have 1, so order is stable but counts should be non-increasing
        for (int i = 1; i < results.Count; i++)
            Assert.True(results[i - 1].IssueCount >= results[i].IssueCount);
    }

    [Fact]
    public async Task GetIssueCountsByCategory_ReturnsAllCategories()
    {
        var results = await _svc.GetIssueCountsByCategoryAsync();

        Assert.Equal(2, results.Count);

        // Pothole: issues 1 and 2
        var pothole = results.First(r => r.CategoryName == "Pothole");
        Assert.Equal(2, pothole.IssueCount);

        // Graffiti: issue 3
        var graffiti = results.First(r => r.CategoryName == "Graffiti");
        Assert.Equal(1, graffiti.IssueCount);
    }

    [Fact]
    public async Task GetIssueCountsByCategory_SortedDescending()
    {
        var results = await _svc.GetIssueCountsByCategoryAsync();

        // Pothole (2) should come before Graffiti (1)
        Assert.Equal("Pothole", results[0].CategoryName);
        Assert.Equal("Graffiti", results[1].CategoryName);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
