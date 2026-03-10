using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Domain.Interfaces;

public interface IIssueRepository : IRepository<Issue>
{
    Task<List<Issue>> GetFilteredAsync(
        int? statusId = null, int? categoryId = null, int? districtId = null,
        int? assignedToUserId = null, IssuePriority? priority = null,
        int? createdByUserId = null, string? searchText = null);

    Task<Issue?> GetByIdWithDetailsAsync(int id);

    Task<Issue?> GetByIdWithChildrenAsync(int id);

    Task<List<Issue>> GetInBoundsAsync(double southLat, double westLng, double northLat, double eastLng);

    Task<List<Status>> GetStatusesAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<List<District>> GetDistrictsAsync();
    Task<List<User>> GetOperatorsAsync();

    Task AddCommentAsync(Comment comment);
    Task AddAttachmentAsync(Attachment attachment);
    Task<Attachment?> GetAttachmentByIdAsync(int id);

    Task<User?> GetUserByEmailAsync(string email);
    Task<Category?> GetCategoryByNameAsync(string name);
    Task<District?> GetDistrictByNameAsync(string name);
}
