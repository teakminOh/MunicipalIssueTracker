using MunicipalIssueTracker.Domain.Interfaces;

namespace MunicipalIssueTracker.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(string fileName, Stream content, string contentType)
    {
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_basePath, safeName);
        using var fs = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fs);
        return safeName;
    }

    public Task<Stream?> GetFileAsync(string storagePath)
    {
        var filePath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(filePath)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteFileAsync(string storagePath)
    {
        var filePath = Path.Combine(_basePath, storagePath);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }
}
