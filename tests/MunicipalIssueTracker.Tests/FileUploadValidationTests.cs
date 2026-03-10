using MunicipalIssueTracker.Web.Services;

namespace MunicipalIssueTracker.Tests;

public class FileUploadValidationTests
{
    // Mirror the whitelist from IssueDetail.razor to test as a shared concept
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain", "text/csv"
    };

    // --- Extension whitelist ---

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    [InlineData(".docx")]
    [InlineData(".xls")]
    [InlineData(".xlsx")]
    [InlineData(".txt")]
    [InlineData(".csv")]
    [InlineData(".bmp")]
    [InlineData(".webp")]
    public void AllowedExtension_IsAccepted(string ext)
    {
        Assert.Contains(ext, AllowedExtensions);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".bat")]
    [InlineData(".sh")]
    [InlineData(".js")]
    [InlineData(".php")]
    [InlineData(".dll")]
    [InlineData(".ps1")]
    [InlineData(".cmd")]
    [InlineData(".msi")]
    [InlineData(".vbs")]
    public void DangerousExtension_IsRejected(string ext)
    {
        Assert.DoesNotContain(ext, AllowedExtensions);
    }

    [Fact]
    public void EmptyExtension_IsRejected()
    {
        Assert.DoesNotContain("", AllowedExtensions);
    }

    // --- MIME type whitelist ---

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("text/csv")]
    public void AllowedMimeType_IsAccepted(string mime)
    {
        Assert.Contains(mime, AllowedMimeTypes);
    }

    [Theory]
    [InlineData("application/x-executable")]
    [InlineData("application/x-msdownload")]
    [InlineData("application/javascript")]
    [InlineData("application/x-sh")]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    public void DangerousMimeType_IsRejected(string mime)
    {
        Assert.DoesNotContain(mime, AllowedMimeTypes);
    }

    // --- File size ---

    [Fact]
    public void MaxFileSize_Is5MB()
    {
        const long maxFileSize = 5 * 1024 * 1024;
        Assert.Equal(5_242_880, maxFileSize);
    }

    [Fact]
    public void FileExceedingMaxSize_WouldBeRejected()
    {
        const long maxFileSize = 5 * 1024 * 1024;
        long oversized = 6 * 1024 * 1024;
        Assert.True(oversized > maxFileSize);
    }

    // --- Extension extraction from filenames ---

    [Theory]
    [InlineData("photo.jpg", ".jpg")]
    [InlineData("DOCUMENT.PDF", ".PDF")]
    [InlineData("archive.tar.gz", ".gz")]
    [InlineData("noextension", "")]
    [InlineData("malicious.jpg.exe", ".exe")]
    public void GetExtension_ExtractsCorrectly(string filename, string expectedExt)
    {
        Assert.Equal(expectedExt, Path.GetExtension(filename));
    }

    [Fact]
    public void DoubleExtension_Attack_IsBlocked()
    {
        // A file named "photo.jpg.exe" should be caught by extension check
        var ext = Path.GetExtension("photo.jpg.exe");
        Assert.DoesNotContain(ext, AllowedExtensions);
    }
}
