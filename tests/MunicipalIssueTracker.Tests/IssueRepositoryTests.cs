using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Infrastructure.Repositories;

namespace MunicipalIssueTracker.Tests;

public class IssueRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IssueRepository _repo;

    public IssueRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        SeedData();
        _repo = new IssueRepository(_db);
    }

    private void SeedData()
    {
        _db.Statuses.AddRange(
            new Status { StatusId = 1, Name = "Submitted", SortOrder = 1 },
            new Status { StatusId = 2, Name = "In Progress", SortOrder = 3 },
            new Status { StatusId = 3, Name = "Closed", SortOrder = 5, IsTerminal = true }
        );
        _db.Categories.AddRange(
            new Category { CategoryId = 1, Name = "Pothole", DefaultPriority = IssuePriority.High },
            new Category { CategoryId = 2, Name = "Graffiti", DefaultPriority = IssuePriority.Low }
        );
        _db.Districts.AddRange(
            new District { DistrictId = 1, Name = "Centrum" },
            new District { DistrictId = 2, Name = "Brehy" }
        );
        _db.Users.AddRange(
            new User { UserId = 1, DisplayName = "Admin", Email = "admin@test.sk", PasswordHash = "x:x", Role = UserRole.Admin, IsActive = true },
            new User { UserId = 2, DisplayName = "Operator", Email = "op@test.sk", PasswordHash = "x:x", Role = UserRole.Operator, IsActive = true },
            new User { UserId = 3, DisplayName = "Citizen", Email = "citizen@test.sk", PasswordHash = "x:x", Role = UserRole.Citizen, IsActive = true },
            new User { UserId = 4, DisplayName = "Inactive Op", Email = "inactive@test.sk", PasswordHash = "x:x", Role = UserRole.Operator, IsActive = false }
        );
        _db.SaveChanges();

        _db.Issues.AddRange(
            new Issue { Title = "Pothole downtown", CategoryId = 1, StatusId = 1, DistrictId = 1, CreatedByUserId = 3, Lat = 49.41, Lng = 19.48, Priority = IssuePriority.High, TrackingCode = "NAM-2026-0001" },
            new Issue { Title = "Graffiti on wall", CategoryId = 2, StatusId = 2, DistrictId = 2, CreatedByUserId = 3, AssignedToUserId = 2, Lat = 49.405, Lng = 19.477, Priority = IssuePriority.Low, TrackingCode = "NAM-2026-0002" },
            new Issue { Title = "Another pothole", CategoryId = 1, StatusId = 1, DistrictId = 1, CreatedByUserId = 1, Lat = 49.408, Lng = 19.484, Priority = IssuePriority.Critical, TrackingCode = "NAM-2026-0003" }
        );
        _db.SaveChanges();
    }

    // --- GetFilteredAsync ---

    [Fact]
    public async Task GetFilteredAsync_NoFilters_ReturnsAll()
    {
        var issues = await _repo.GetFilteredAsync();
        Assert.Equal(3, issues.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_ByStatus_FiltersCorrectly()
    {
        var issues = await _repo.GetFilteredAsync(statusId: 1);
        Assert.Equal(2, issues.Count);
        Assert.All(issues, i => Assert.Equal(1, i.StatusId));
    }

    [Fact]
    public async Task GetFilteredAsync_ByCategory_FiltersCorrectly()
    {
        var issues = await _repo.GetFilteredAsync(categoryId: 2);
        Assert.Single(issues);
        Assert.Equal("Graffiti on wall", issues[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_ByDistrict_FiltersCorrectly()
    {
        var issues = await _repo.GetFilteredAsync(districtId: 2);
        Assert.Single(issues);
    }

    [Fact]
    public async Task GetFilteredAsync_ByPriority_FiltersCorrectly()
    {
        var issues = await _repo.GetFilteredAsync(priority: IssuePriority.Critical);
        Assert.Single(issues);
        Assert.Equal("Another pothole", issues[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_ByCreatedBy_FiltersCorrectly()
    {
        var issues = await _repo.GetFilteredAsync(createdByUserId: 3);
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_BySearchText_MatchesTitle()
    {
        var issues = await _repo.GetFilteredAsync(searchText: "graffiti");
        Assert.Single(issues);
        Assert.Equal("Graffiti on wall", issues[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_BySearchText_MatchesTrackingCode()
    {
        var issues = await _repo.GetFilteredAsync(searchText: "NAM-2026-0003");
        Assert.Single(issues);
    }

    [Fact]
    public async Task GetFilteredAsync_CombinedFilters_WorkTogether()
    {
        var issues = await _repo.GetFilteredAsync(statusId: 1, categoryId: 1);
        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_NoMatches_ReturnsEmpty()
    {
        var issues = await _repo.GetFilteredAsync(statusId: 99);
        Assert.Empty(issues);
    }

    [Fact]
    public async Task GetFilteredAsync_ResultsOrderedByCreatedAtDescending()
    {
        var issues = await _repo.GetFilteredAsync();
        for (int i = 0; i < issues.Count - 1; i++)
            Assert.True(issues[i].CreatedAt >= issues[i + 1].CreatedAt);
    }

    // --- GetByIdWithDetailsAsync ---

    [Fact]
    public async Task GetByIdWithDetailsAsync_ExistingId_IncludesNavigationProperties()
    {
        var all = await _repo.GetFilteredAsync();
        var issue = await _repo.GetByIdWithDetailsAsync(all.First().IssueId);

        Assert.NotNull(issue);
        Assert.NotNull(issue!.Category);
        Assert.NotNull(issue.Status);
        Assert.NotNull(issue.District);
        Assert.NotNull(issue.CreatedBy);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
    {
        var issue = await _repo.GetByIdWithDetailsAsync(9999);
        Assert.Null(issue);
    }

    // --- GetInBoundsAsync ---

    [Fact]
    public async Task GetInBoundsAsync_ContainingAll_ReturnsAll()
    {
        var issues = await _repo.GetInBoundsAsync(49.0, 19.0, 50.0, 20.0);
        Assert.Equal(3, issues.Count);
    }

    [Fact]
    public async Task GetInBoundsAsync_NarrowBounds_ReturnsSome()
    {
        // Only the first issue (49.41, 19.48) should fit
        var issues = await _repo.GetInBoundsAsync(49.409, 19.479, 49.411, 19.481);
        Assert.Single(issues);
    }

    [Fact]
    public async Task GetInBoundsAsync_EmptyBounds_ReturnsNone()
    {
        var issues = await _repo.GetInBoundsAsync(50.0, 20.0, 51.0, 21.0);
        Assert.Empty(issues);
    }

    // --- Lookups ---

    [Fact]
    public async Task GetStatusesAsync_ReturnsSortedByOrder()
    {
        var statuses = await _repo.GetStatusesAsync();
        Assert.Equal(3, statuses.Count);
        Assert.True(statuses[0].SortOrder <= statuses[1].SortOrder);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsSortedByName()
    {
        var categories = await _repo.GetCategoriesAsync();
        Assert.Equal(2, categories.Count);
        Assert.True(string.Compare(categories[0].Name, categories[1].Name, StringComparison.Ordinal) <= 0);
    }

    [Fact]
    public async Task GetDistrictsAsync_ReturnsAll()
    {
        var districts = await _repo.GetDistrictsAsync();
        Assert.Equal(2, districts.Count);
    }

    [Fact]
    public async Task GetOperatorsAsync_ReturnsOnlyActiveOperatorsAndAdmins()
    {
        var operators = await _repo.GetOperatorsAsync();
        Assert.Equal(2, operators.Count); // Admin + active Operator (not citizen, not inactive)
        Assert.All(operators, u => Assert.True(u.Role == UserRole.Admin || u.Role == UserRole.Operator));
        Assert.All(operators, u => Assert.True(u.IsActive));
    }

    // --- Comments & Attachments ---

    [Fact]
    public async Task AddCommentAsync_SetsCreatedAtAndPersists()
    {
        var all = await _repo.GetFilteredAsync();
        var issueId = all.First().IssueId;

        await _repo.AddCommentAsync(new Comment
        {
            IssueId = issueId,
            AuthorUserId = 1,
            Body = "Test comment"
        });

        var issue = await _repo.GetByIdWithDetailsAsync(issueId);
        Assert.Single(issue!.Comments);
        Assert.Equal("Test comment", issue.Comments.First().Body);
        Assert.True(issue.Comments.First().CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddAttachmentAsync_SetsUploadedAtAndPersists()
    {
        var all = await _repo.GetFilteredAsync();
        var issueId = all.First().IssueId;

        await _repo.AddAttachmentAsync(new Attachment
        {
            IssueId = issueId,
            FileName = "test.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            StoragePath = "abc123_test.pdf"
        });

        var issue = await _repo.GetByIdWithDetailsAsync(issueId);
        Assert.Single(issue!.Attachments);
        Assert.Equal("test.pdf", issue.Attachments.First().FileName);
    }

    [Fact]
    public async Task GetAttachmentByIdAsync_ExistingId_Returns()
    {
        var all = await _repo.GetFilteredAsync();
        var issueId = all.First().IssueId;

        await _repo.AddAttachmentAsync(new Attachment
        {
            IssueId = issueId,
            FileName = "photo.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 2048,
            StoragePath = "abc_photo.jpg"
        });

        var attachments = (await _repo.GetByIdWithDetailsAsync(issueId))!.Attachments;
        var attId = attachments.First().AttachmentId;

        var att = await _repo.GetAttachmentByIdAsync(attId);
        Assert.NotNull(att);
        Assert.Equal("photo.jpg", att!.FileName);
    }

    // --- User/Category/District lookups ---

    [Fact]
    public async Task GetUserByEmailAsync_ExistingActive_ReturnsUser()
    {
        var user = await _repo.GetUserByEmailAsync("admin@test.sk");
        Assert.NotNull(user);
        Assert.Equal("Admin", user!.DisplayName);
    }

    [Fact]
    public async Task GetUserByEmailAsync_InactiveUser_ReturnsNull()
    {
        var user = await _repo.GetUserByEmailAsync("inactive@test.sk");
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExisting_ReturnsNull()
    {
        var user = await _repo.GetUserByEmailAsync("nobody@test.sk");
        Assert.Null(user);
    }

    [Fact]
    public async Task GetCategoryByNameAsync_Existing_ReturnsCategory()
    {
        var cat = await _repo.GetCategoryByNameAsync("Pothole");
        Assert.NotNull(cat);
    }

    [Fact]
    public async Task GetCategoryByNameAsync_NonExisting_ReturnsNull()
    {
        var cat = await _repo.GetCategoryByNameAsync("NonExistent");
        Assert.Null(cat);
    }

    [Fact]
    public async Task GetDistrictByNameAsync_Existing_ReturnsDistrict()
    {
        var dist = await _repo.GetDistrictByNameAsync("Centrum");
        Assert.NotNull(dist);
    }

    // --- CRUD ---

    [Fact]
    public async Task AddAsync_PersistsIssue()
    {
        var newIssue = new Issue
        {
            Title = "New issue",
            CategoryId = 1,
            StatusId = 1,
            DistrictId = 1,
            CreatedByUserId = 1,
            Lat = 49.40,
            Lng = 19.47,
            Priority = IssuePriority.Medium,
            TrackingCode = "NAM-2026-0099"
        };

        await _repo.AddAsync(newIssue);
        Assert.True(newIssue.IssueId > 0);

        var all = await _repo.GetFilteredAsync();
        Assert.Equal(4, all.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesIssue()
    {
        var all = await _repo.GetFilteredAsync();
        var issue = all.First();

        await _repo.DeleteAsync(issue);

        var remaining = await _repo.GetFilteredAsync();
        Assert.Equal(2, remaining.Count);
    }

    public void Dispose() => _db.Dispose();
}
