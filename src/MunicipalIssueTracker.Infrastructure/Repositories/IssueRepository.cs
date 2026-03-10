using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Domain.Interfaces;
using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Infrastructure.Repositories;

public class IssueRepository : Repository<Issue>, IIssueRepository
{
    public IssueRepository(AppDbContext db) : base(db) { }

    public async Task<List<Issue>> GetFilteredAsync(
        int? statusId = null, int? categoryId = null, int? districtId = null,
        int? assignedToUserId = null, IssuePriority? priority = null,
        int? createdByUserId = null, string? searchText = null)
    {
        var query = Db.Issues
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
        if (createdByUserId.HasValue) query = query.Where(i => i.CreatedByUserId == createdByUserId.Value);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim().ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(term)
                || i.TrackingCode.ToLower().Contains(term)
                || i.Description.ToLower().Contains(term));
        }

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task<Issue?> GetByIdWithDetailsAsync(int id)
    {
        return await Db.Issues
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

    public async Task<Issue?> GetByIdWithChildrenAsync(int id)
    {
        return await Db.Issues
            .Include(i => i.Comments)
            .Include(i => i.Attachments)
            .Include(i => i.AuditLogs)
            .FirstOrDefaultAsync(i => i.IssueId == id);
    }

    public async Task<List<Issue>> GetInBoundsAsync(double southLat, double westLng, double northLat, double eastLng)
    {
        return await Db.Issues
            .Include(i => i.Category)
            .Include(i => i.Status)
            .Where(i => i.Lat >= southLat && i.Lat <= northLat && i.Lng >= westLng && i.Lng <= eastLng)
            .ToListAsync();
    }

    public async Task<List<Status>> GetStatusesAsync() =>
        await Db.Statuses.OrderBy(s => s.SortOrder).ToListAsync();

    public async Task<List<Category>> GetCategoriesAsync() =>
        await Db.Categories.OrderBy(c => c.Name).ToListAsync();

    public async Task<List<District>> GetDistrictsAsync() =>
        await Db.Districts.OrderBy(d => d.Name).ToListAsync();

    public async Task<List<User>> GetOperatorsAsync() =>
        await Db.Users.Where(u => u.IsActive && (u.Role == UserRole.Operator || u.Role == UserRole.Admin))
            .OrderBy(u => u.DisplayName).ToListAsync();

    public async Task AddCommentAsync(Comment comment)
    {
        comment.CreatedAt = DateTime.UtcNow;
        Db.Comments.Add(comment);
        await Db.SaveChangesAsync();
    }

    public async Task AddAttachmentAsync(Attachment attachment)
    {
        attachment.UploadedAt = DateTime.UtcNow;
        Db.Attachments.Add(attachment);
        await Db.SaveChangesAsync();
    }

    public async Task<Attachment?> GetAttachmentByIdAsync(int id) =>
        await Db.Attachments.FindAsync(id);

    public async Task<User?> GetUserByEmailAsync(string email) =>
        await Db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

    public async Task<Category?> GetCategoryByNameAsync(string name) =>
        await Db.Categories.FirstOrDefaultAsync(c => c.Name == name);

    public async Task<District?> GetDistrictByNameAsync(string name) =>
        await Db.Districts.FirstOrDefaultAsync(d => d.Name == name);
}
