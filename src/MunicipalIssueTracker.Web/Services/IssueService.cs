using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Domain.Interfaces;

namespace MunicipalIssueTracker.Web.Services;

public class IssueService
{
    private readonly IIssueRepository _repo;
    private readonly ILogger<IssueService> _logger;

    public IssueService(IIssueRepository repo, ILogger<IssueService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<Issue>> GetIssuesAsync(
        int? statusId = null, int? categoryId = null, int? districtId = null,
        int? assignedToUserId = null, IssuePriority? priority = null, int? createdByUserId = null,
        string? searchText = null)
    {
        _logger.LogDebug("Fetching issues with filters: status={StatusId}, category={CategoryId}, district={DistrictId}",
            statusId, categoryId, districtId);
        return await _repo.GetFilteredAsync(statusId, categoryId, districtId, assignedToUserId, priority, createdByUserId, searchText);
    }

    public async Task<Issue?> GetIssueByIdAsync(int id)
    {
        return await _repo.GetByIdWithDetailsAsync(id);
    }

    public async Task<Issue> CreateIssueAsync(Issue issue)
    {
        issue.CreatedAt = DateTime.UtcNow;
        await _repo.AddAsync(issue);

        // Generate tracking code after save so IssueId is assigned
        issue.TrackingCode = $"NAM-{issue.CreatedAt.Year}-{issue.IssueId:D4}";
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Issue {IssueId} created with tracking code {TrackingCode}", issue.IssueId, issue.TrackingCode);
        return issue;
    }

    public async Task UpdateIssueAsync(Issue issue)
    {
        await _repo.UpdateAsync(issue);
        _logger.LogInformation("Issue {IssueId} updated", issue.IssueId);
    }

    public async Task DeleteIssueAsync(int issueId)
    {
        var issue = await _repo.GetByIdWithChildrenAsync(issueId);
        if (issue == null) return;

        await _repo.DeleteAsync(issue);
        _logger.LogInformation("Issue {IssueId} deleted", issueId);
    }

    public async Task<List<Status>> GetStatusesAsync() => await _repo.GetStatusesAsync();
    public async Task<List<Category>> GetCategoriesAsync() => await _repo.GetCategoriesAsync();
    public async Task<List<District>> GetDistrictsAsync() => await _repo.GetDistrictsAsync();
    public async Task<List<User>> GetOperatorsAsync() => await _repo.GetOperatorsAsync();

    public async Task AddCommentAsync(Comment comment)
    {
        await _repo.AddCommentAsync(comment);
        _logger.LogInformation("Comment added to issue {IssueId}", comment.IssueId);
    }

    public async Task<List<Issue>> GetIssuesInBoundsAsync(double southLat, double westLng, double northLat, double eastLng)
    {
        return await _repo.GetInBoundsAsync(southLat, westLng, northLat, eastLng);
    }

    public async Task AddAttachmentAsync(Attachment attachment)
    {
        await _repo.AddAttachmentAsync(attachment);
        _logger.LogInformation("Attachment {FileName} added to issue {IssueId}", attachment.FileName, attachment.IssueId);
    }

    // Status transition rules: from StatusId → allowed target StatusIds
    private static readonly Dictionary<int, int[]> AllowedTransitions = new()
    {
        [1] = [2, 6, 7],       // Submitted → Confirmed, Rejected, Duplicate
        [2] = [3, 4, 6, 7],   // Confirmed → In Progress, Resolved, Rejected, Duplicate
        [3] = [4, 2],          // In Progress → Resolved, Confirmed (revert)
        [4] = [5, 3],          // Resolved → Closed, In Progress (reopen)
        [5] = [],              // Closed → terminal
        [6] = [],              // Rejected → terminal
        [7] = [],              // Duplicate → terminal
    };

    public int[] GetAllowedNextStatuses(int currentStatusId)
    {
        return AllowedTransitions.TryGetValue(currentStatusId, out var allowed) ? allowed : [];
    }

    public bool IsTransitionAllowed(int fromStatusId, int toStatusId)
    {
        if (fromStatusId == toStatusId) return true; // no-op is always fine
        return AllowedTransitions.TryGetValue(fromStatusId, out var allowed) && allowed.Contains(toStatusId);
    }

    // Statuses that require a resolution note
    private static readonly HashSet<int> RequiresResolutionNote = [4, 6]; // Resolved, Rejected

    public bool StatusRequiresResolutionNote(int statusId) => RequiresResolutionNote.Contains(statusId);
}
