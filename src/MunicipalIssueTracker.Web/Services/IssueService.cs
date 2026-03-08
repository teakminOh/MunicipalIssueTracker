using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Web.Services;

public class IssueService
{
    private readonly AppDbContext _db;

    public IssueService(AppDbContext db) => _db = db;

    public async Task<List<Issue>> GetIssuesAsync(
        int? statusId = null, int? categoryId = null, int? districtId = null,
        int? assignedToUserId = null, IssuePriority? priority = null)
    {
        var query = _db.Issues
            .Include(i => i.Category)
            .Include(i => i.Status)
            .Include(i => i.District)
            .Include(i => i.AssignedTo)
            .Include(i => i.CreatedBy)
            .AsQueryable();

        if (statusId.HasValue) query = query.Where(i => i.StatusId == statusId.Value);
        if (categoryId.HasValue) query = query.Where(i => i.CategoryId == categoryId.Value);
        if (districtId.HasValue) query = query.Where(i => i.DistrictId == districtId.Value);
        if (assignedToUserId.HasValue) query = query.Where(i => i.AssignedToUserId == assignedToUserId.Value);
        if (priority.HasValue) query = query.Where(i => i.Priority == priority.Value);

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task<Issue?> GetIssueByIdAsync(int id)
    {
        return await _db.Issues
            .Include(i => i.Category)
            .Include(i => i.Status)
            .Include(i => i.District)
            .Include(i => i.AssignedTo)
            .Include(i => i.CreatedBy)
            .Include(i => i.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .Include(i => i.Attachments)
            .Include(i => i.AuditLogs.OrderByDescending(a => a.CreatedAt))
                .ThenInclude(a => a.Actor)
            .FirstOrDefaultAsync(i => i.IssueId == id);
    }

    public async Task<Issue> CreateIssueAsync(Issue issue)
    {
        issue.CreatedAt = DateTime.UtcNow;
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();
        return issue;
    }

    public async Task UpdateIssueAsync(Issue issue)
    {
        _db.Issues.Update(issue);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Status>> GetStatusesAsync() =>
        await _db.Statuses.OrderBy(s => s.SortOrder).ToListAsync();

    public async Task<List<Category>> GetCategoriesAsync() =>
        await _db.Categories.OrderBy(c => c.Name).ToListAsync();

    public async Task<List<District>> GetDistrictsAsync() =>
        await _db.Districts.OrderBy(d => d.Name).ToListAsync();

    public async Task<List<User>> GetOperatorsAsync() =>
        await _db.Users.Where(u => u.IsActive && (u.Role == UserRole.Operator || u.Role == UserRole.Admin))
            .OrderBy(u => u.DisplayName).ToListAsync();

    public async Task AddCommentAsync(Comment comment)
    {
        comment.CreatedAt = DateTime.UtcNow;
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Bounding-box query for map viewport filtering.
    /// Demonstrates spatial-style query with raw coordinate comparison.
    /// </summary>
    public async Task<List<Issue>> GetIssuesInBoundsAsync(double southLat, double westLng, double northLat, double eastLng)
    {
        return await _db.Issues
            .Include(i => i.Category)
            .Include(i => i.Status)
            .Where(i => i.Lat >= southLat && i.Lat <= northLat && i.Lng >= westLng && i.Lng <= eastLng)
            .ToListAsync();
    }

    public async Task AddAttachmentAsync(Attachment attachment)
    {
        attachment.UploadedAt = DateTime.UtcNow;
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
    }
}
