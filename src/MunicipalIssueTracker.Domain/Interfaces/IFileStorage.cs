namespace MunicipalIssueTracker.Domain.Interfaces;

public interface IFileStorage
{
    Task<string> SaveFileAsync(string fileName, Stream content, string contentType);
    Task<Stream?> GetFileAsync(string storagePath);
    Task DeleteFileAsync(string storagePath);
}
