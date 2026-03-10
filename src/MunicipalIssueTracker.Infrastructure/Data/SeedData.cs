using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;

namespace MunicipalIssueTracker.Infrastructure.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.Migrate();

        if (context.Users.Any()) return; // Already seeded

        // Statuses
        var statuses = new[]
        {
            new Status { StatusId = 1, Name = "Submitted", SortOrder = 1, IsTerminal = false },
            new Status { StatusId = 2, Name = "Confirmed", SortOrder = 2, IsTerminal = false },
            new Status { StatusId = 3, Name = "In Progress", SortOrder = 3, IsTerminal = false },
            new Status { StatusId = 4, Name = "Resolved", SortOrder = 4, IsTerminal = false },
            new Status { StatusId = 5, Name = "Closed", SortOrder = 5, IsTerminal = true },
            new Status { StatusId = 6, Name = "Rejected", SortOrder = 6, IsTerminal = true },
            new Status { StatusId = 7, Name = "Duplicate", SortOrder = 7, IsTerminal = true },
        };
        context.Statuses.AddRange(statuses);

        // Districts (Námestovo and surrounding areas in Orava region)
        var districts = new[]
        {
            new District { DistrictId = 1, Name = "Stred" },
            new District { DistrictId = 2, Name = "Brehy" },
            new District { DistrictId = 3, Name = "Čerchle" },
            new District { DistrictId = 4, Name = "Predmostie" },
            new District { DistrictId = 5, Name = "Vojenské" },
            new District { DistrictId = 6, Name = "Priemyselná zóna" },
            new District { DistrictId = 7, Name = "Slanica I" },
            new District { DistrictId = 8, Name = "Slanica II" },
            new District { DistrictId = 9, Name = "Slanická Osada" },
        };
        context.Districts.AddRange(districts);

        // Categories
        var categories = new[]
        {
            new Category { CategoryId = 1, Name = "Pothole", Icon = "warning", DefaultPriority = IssuePriority.High },
            new Category { CategoryId = 2, Name = "Broken Street Light", Icon = "lightbulb", DefaultPriority = IssuePriority.Medium },
            new Category { CategoryId = 3, Name = "Waste / Littering", Icon = "delete", DefaultPriority = IssuePriority.Medium },
            new Category { CategoryId = 4, Name = "Greenery / Parks", Icon = "park", DefaultPriority = IssuePriority.Low },
            new Category { CategoryId = 5, Name = "Road Damage", Icon = "construction", DefaultPriority = IssuePriority.High },
            new Category { CategoryId = 6, Name = "Graffiti / Vandalism", Icon = "format_paint", DefaultPriority = IssuePriority.Low },
            new Category { CategoryId = 7, Name = "Water / Drainage", Icon = "water_drop", DefaultPriority = IssuePriority.Critical },
            new Category { CategoryId = 8, Name = "Other", Icon = "report", DefaultPriority = IssuePriority.Medium },
        };
        context.Categories.AddRange(categories);

        // Users (passwords hashed with simple PBKDF2 — demo purposes)
        var users = new[]
        {
            new User { UserId = 1, DisplayName = "Admin Používateľ", Email = "admin@namestovo.sk", Role = UserRole.Admin, PasswordHash = HashPassword("Admin123!"), IsActive = true },
            new User { UserId = 2, DisplayName = "Ján Kováč", Email = "jan.kovac@namestovo.sk", Role = UserRole.Operator, PasswordHash = HashPassword("Operator123!"), IsActive = true },
            new User { UserId = 3, DisplayName = "Mária Horváthová", Email = "maria.horvathova@namestovo.sk", Role = UserRole.Operator, PasswordHash = HashPassword("Operator123!"), IsActive = true },
            new User { UserId = 4, DisplayName = "Anna Kučerová", Email = "anna.kucerova@namestovo.sk", Role = UserRole.Citizen, PasswordHash = HashPassword("Citizen123!"), IsActive = true },
        };
        context.Users.AddRange(users);
        context.SaveChanges();
    }

    public static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        // Store as salt:hash in base64
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
