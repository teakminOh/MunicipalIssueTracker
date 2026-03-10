using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Infrastructure.Repositories;
using MunicipalIssueTracker.Web.Services;

namespace MunicipalIssueTracker.Tests;

public class StatusTransitionTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IssueService _svc;

    public StatusTransitionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        SeedData();
        var repo = new IssueRepository(_db);
        _svc = new IssueService(repo, NullLogger<IssueService>.Instance);
    }

    private void SeedData()
    {
        _db.Statuses.AddRange(
            new Status { StatusId = 1, Name = "Submitted", SortOrder = 1 },
            new Status { StatusId = 2, Name = "Confirmed", SortOrder = 2 },
            new Status { StatusId = 3, Name = "In Progress", SortOrder = 3 },
            new Status { StatusId = 4, Name = "Resolved", SortOrder = 4 },
            new Status { StatusId = 5, Name = "Closed", SortOrder = 5, IsTerminal = true },
            new Status { StatusId = 6, Name = "Rejected", SortOrder = 6, IsTerminal = true },
            new Status { StatusId = 7, Name = "Duplicate", SortOrder = 7, IsTerminal = true }
        );
        _db.SaveChanges();
    }

    // --- IsTransitionAllowed ---

    [Theory]
    [InlineData(1, 2)] // Submitted → Confirmed
    [InlineData(1, 6)] // Submitted → Rejected
    [InlineData(1, 7)] // Submitted → Duplicate
    [InlineData(2, 3)] // Confirmed → In Progress
    [InlineData(2, 4)] // Confirmed → Resolved
    [InlineData(2, 6)] // Confirmed → Rejected
    [InlineData(2, 7)] // Confirmed → Duplicate
    [InlineData(3, 4)] // In Progress → Resolved
    [InlineData(3, 2)] // In Progress → Confirmed (revert)
    [InlineData(4, 5)] // Resolved → Closed
    [InlineData(4, 3)] // Resolved → In Progress (reopen)
    public void IsTransitionAllowed_ValidTransitions_ReturnsTrue(int from, int to)
    {
        Assert.True(_svc.IsTransitionAllowed(from, to));
    }

    [Theory]
    [InlineData(1, 3)] // Submitted → In Progress (must be confirmed first)
    [InlineData(1, 4)] // Submitted → Resolved (can't skip)
    [InlineData(1, 5)] // Submitted → Closed (can't skip)
    [InlineData(3, 5)] // In Progress → Closed (must resolve first)
    [InlineData(5, 1)] // Closed → Submitted (terminal)
    [InlineData(5, 3)] // Closed → In Progress (terminal)
    [InlineData(6, 1)] // Rejected → Submitted (terminal)
    [InlineData(7, 1)] // Duplicate → Submitted (terminal)
    public void IsTransitionAllowed_InvalidTransitions_ReturnsFalse(int from, int to)
    {
        Assert.False(_svc.IsTransitionAllowed(from, to));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void IsTransitionAllowed_SameStatus_AlwaysTrue(int status)
    {
        Assert.True(_svc.IsTransitionAllowed(status, status));
    }

    // --- GetAllowedNextStatuses ---

    [Fact]
    public void GetAllowedNextStatuses_FromSubmitted_Returns_Confirmed_Rejected_Duplicate()
    {
        var allowed = _svc.GetAllowedNextStatuses(1);
        Assert.Contains(2, allowed);
        Assert.Contains(6, allowed);
        Assert.Contains(7, allowed);
        Assert.Equal(3, allowed.Length);
    }

    [Fact]
    public void GetAllowedNextStatuses_FromTerminal_ReturnsEmpty()
    {
        Assert.Empty(_svc.GetAllowedNextStatuses(5)); // Closed
        Assert.Empty(_svc.GetAllowedNextStatuses(6)); // Rejected
        Assert.Empty(_svc.GetAllowedNextStatuses(7)); // Duplicate
    }

    [Fact]
    public void GetAllowedNextStatuses_UnknownStatus_ReturnsEmpty()
    {
        Assert.Empty(_svc.GetAllowedNextStatuses(999));
    }

    // --- StatusRequiresResolutionNote ---

    [Theory]
    [InlineData(4, true)]  // Resolved
    [InlineData(6, true)]  // Rejected
    public void StatusRequiresResolutionNote_RequiredStatuses(int statusId, bool expected)
    {
        Assert.Equal(expected, _svc.StatusRequiresResolutionNote(statusId));
    }

    [Theory]
    [InlineData(1)]  // Submitted
    [InlineData(2)]  // Confirmed
    [InlineData(3)]  // In Progress
    [InlineData(5)]  // Closed
    [InlineData(7)]  // Duplicate
    public void StatusRequiresResolutionNote_NotRequired(int statusId)
    {
        Assert.False(_svc.StatusRequiresResolutionNote(statusId));
    }

    public void Dispose() => _db.Dispose();
}
