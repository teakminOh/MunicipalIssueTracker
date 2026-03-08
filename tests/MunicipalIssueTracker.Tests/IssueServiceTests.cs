using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Web.Services;

namespace MunicipalIssueTracker.Tests;

public class IssueServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IssueService _svc;

    public IssueServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        SeedTestData();
        _svc = new IssueService(_db);
    }

    private void SeedTestData()
    {
        _db.Statuses.AddRange(
            new Status { StatusId = 1, Name = "Reported", SortOrder = 1 },
            new Status { StatusId = 2, Name = "In Progress", SortOrder = 3 }
        );
        _db.Categories.Add(new Category { CategoryId = 1, Name = "Pothole", DefaultPriority = IssuePriority.High });
        _db.Districts.Add(new District { DistrictId = 1, Name = "Centrum" });
        _db.Users.Add(new User { UserId = 1, DisplayName = "Test", Email = "test@namestovo.sk", PasswordHash = "x:x", Role = UserRole.Admin });
        _db.SaveChanges();

        _db.Issues.AddRange(
            new Issue { Title = "Issue A", CategoryId = 1, StatusId = 1, DistrictId = 1, CreatedByUserId = 1, Lat = 49.41, Lng = 19.48, Priority = IssuePriority.High },
            new Issue { Title = "Issue B", CategoryId = 1, StatusId = 2, DistrictId = 1, CreatedByUserId = 1, Lat = 49.42, Lng = 19.49, Priority = IssuePriority.Low }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetIssuesAsync_ReturnsAllIssues()
    {
        var issues = await _svc.GetIssuesAsync();
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task GetIssuesAsync_FilterByStatus_ReturnsFiltered()
    {
        var issues = await _svc.GetIssuesAsync(statusId: 1);
        Assert.Single(issues);
        Assert.Equal("Issue A", issues[0].Title);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ExistingId_ReturnsIssue()
    {
        var all = await _svc.GetIssuesAsync();
        var id = all.First().IssueId;
        var issue = await _svc.GetIssueByIdAsync(id);
        Assert.NotNull(issue);
    }

    [Fact]
    public async Task GetIssueByIdAsync_NonExistingId_ReturnsNull()
    {
        var issue = await _svc.GetIssueByIdAsync(9999);
        Assert.Null(issue);
    }

    [Fact]
    public async Task CreateIssueAsync_AddNewIssue()
    {
        var newIssue = new Issue
        {
            Title = "New Issue",
            CategoryId = 1,
            StatusId = 1,
            DistrictId = 1,
            CreatedByUserId = 1,
            Lat = 49.40,
            Lng = 19.47,
            Priority = IssuePriority.Medium
        };

        var created = await _svc.CreateIssueAsync(newIssue);
        Assert.True(created.IssueId > 0);
        Assert.Equal(3, (await _svc.GetIssuesAsync()).Count);
    }

    [Fact]
    public async Task AddCommentAsync_AddsComment()
    {
        var all = await _svc.GetIssuesAsync();
        var issueId = all.First().IssueId;

        await _svc.AddCommentAsync(new Comment
        {
            IssueId = issueId,
            AuthorUserId = 1,
            Body = "Test comment"
        });

        var issue = await _svc.GetIssueByIdAsync(issueId);
        Assert.Single(issue!.Comments);
        Assert.Equal("Test comment", issue.Comments.First().Body);
    }

    [Fact]
    public async Task GetIssuesInBoundsAsync_FiltersCorrectly()
    {
        var issues = await _svc.GetIssuesInBoundsAsync(49.405, 19.475, 49.415, 19.485);
        Assert.Single(issues); // Only Issue A is within these bounds
    }

    public void Dispose() => _db.Dispose();
}
