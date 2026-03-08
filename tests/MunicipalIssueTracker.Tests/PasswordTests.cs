using MunicipalIssueTracker.Infrastructure.Data;

namespace MunicipalIssueTracker.Tests;

public class PasswordTests
{
    [Fact]
    public void HashPassword_ProducesNonEmptyHash()
    {
        var hash = SeedData.HashPassword("Test123!");
        Assert.NotEmpty(hash);
        Assert.Contains(":", hash);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var hash = SeedData.HashPassword("Test123!");
        Assert.True(SeedData.VerifyPassword("Test123!", hash));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = SeedData.HashPassword("Test123!");
        Assert.False(SeedData.VerifyPassword("WrongPassword!", hash));
    }

    [Fact]
    public void HashPassword_DifferentInputs_ProduceDifferentHashes()
    {
        var hash1 = SeedData.HashPassword("Password1");
        var hash2 = SeedData.HashPassword("Password2");
        Assert.NotEqual(hash1, hash2);
    }
}
