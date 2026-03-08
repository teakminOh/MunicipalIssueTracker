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
            new Status { StatusId = 1, Name = "Reported", SortOrder = 1 },
            new Status { StatusId = 2, Name = "Confirmed", SortOrder = 2 },
            new Status { StatusId = 3, Name = "In Progress", SortOrder = 3 },
            new Status { StatusId = 4, Name = "Resolved", SortOrder = 4 },
            new Status { StatusId = 5, Name = "Closed", SortOrder = 5 },
        };
        context.Statuses.AddRange(statuses);

        // Districts (realistic Swedish-style municipality districts)
        var districts = new[]
        {
            new District { DistrictId = 1, Name = "Centrum" },
            new District { DistrictId = 2, Name = "Norrmalm" },
            new District { DistrictId = 3, Name = "Södermalm" },
            new District { DistrictId = 4, Name = "Östermalm" },
            new District { DistrictId = 5, Name = "Kungsholmen" },
            new District { DistrictId = 6, Name = "Vasastan" },
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
            new User { UserId = 1, DisplayName = "Admin User", Email = "admin@municipality.se", Role = UserRole.Admin, PasswordHash = HashPassword("Admin123!"), IsActive = true },
            new User { UserId = 2, DisplayName = "Erik Johansson", Email = "erik@municipality.se", Role = UserRole.Operator, PasswordHash = HashPassword("Operator123!"), IsActive = true },
            new User { UserId = 3, DisplayName = "Maria Lindström", Email = "maria@municipality.se", Role = UserRole.Operator, PasswordHash = HashPassword("Operator123!"), IsActive = true },
            new User { UserId = 4, DisplayName = "Anna Svensson", Email = "anna@municipality.se", Role = UserRole.Viewer, PasswordHash = HashPassword("Viewer123!"), IsActive = true },
        };
        context.Users.AddRange(users);
        context.SaveChanges();

        // Issues — Stockholm-area coordinates for realism
        var issues = new[]
        {
            new Issue { Title = "Large pothole on Kungsgatan", Description = "Deep pothole near intersection with Sveavägen, approximately 30cm diameter. Risk for cyclists.", CategoryId = 1, StatusId = 1, DistrictId = 1, Lat = 59.3349, Lng = 18.0686, AddressText = "Kungsgatan 44, Stockholm", CreatedByUserId = 4, Priority = IssuePriority.High, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new Issue { Title = "Street light out on Götgatan", Description = "Street light #SL-2847 has been non-functional for 3 days. Intersection poorly lit at night.", CategoryId = 2, StatusId = 2, DistrictId = 3, Lat = 59.3155, Lng = 18.0730, AddressText = "Götgatan 18, Stockholm", CreatedByUserId = 4, AssignedToUserId = 2, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Issue { Title = "Overflowing waste bin at Humlegården", Description = "The public waste bin at the south entrance of Humlegården has been overflowing since Monday.", CategoryId = 3, StatusId = 3, DistrictId = 4, Lat = 59.3400, Lng = 18.0750, AddressText = "Humlegården, Stockholm", CreatedByUserId = 4, AssignedToUserId = 3, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Issue { Title = "Fallen tree in Rålambshovsparken", Description = "Large oak tree fallen across the main path after storm. Blocking pedestrian and cyclist access.", CategoryId = 4, StatusId = 3, DistrictId = 5, Lat = 59.3270, Lng = 18.0450, AddressText = "Rålambshovsparken, Stockholm", CreatedByUserId = 1, AssignedToUserId = 2, Priority = IssuePriority.High, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Issue { Title = "Cracked pavement on Odengatan", Description = "Multiple cracks in pavement near bus stop. Trip hazard for elderly pedestrians.", CategoryId = 5, StatusId = 1, DistrictId = 6, Lat = 59.3460, Lng = 18.0530, AddressText = "Odengatan 72, Stockholm", CreatedByUserId = 4, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Issue { Title = "Graffiti on Norrmalm school wall", Description = "Large graffiti spray-painted on south-facing wall of the school building. Contains inappropriate imagery.", CategoryId = 6, StatusId = 2, DistrictId = 2, Lat = 59.3380, Lng = 18.0600, AddressText = "Norrmalm, Stockholm", CreatedByUserId = 4, AssignedToUserId = 3, Priority = IssuePriority.Low, CreatedAt = DateTime.UtcNow.AddDays(-6) },
            new Issue { Title = "Blocked drain on Hornsgatan", Description = "Storm drain is completely blocked. Water pooling on the road during rain, causing traffic hazard.", CategoryId = 7, StatusId = 1, DistrictId = 3, Lat = 59.3180, Lng = 18.0500, AddressText = "Hornsgatan 82, Stockholm", CreatedByUserId = 1, Priority = IssuePriority.Critical, CreatedAt = DateTime.UtcNow.AddHours(-6) },
            new Issue { Title = "Damaged bench in Vasaparken", Description = "Wooden slats broken on the bench near the playground. Sharp edges dangerous for children.", CategoryId = 4, StatusId = 4, DistrictId = 6, Lat = 59.3430, Lng = 18.0440, AddressText = "Vasaparken, Stockholm", CreatedByUserId = 4, AssignedToUserId = 2, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Issue { Title = "Illegal dumping near Skanstull", Description = "Furniture and construction waste dumped on the sidewalk. Partially blocking pedestrian path.", CategoryId = 3, StatusId = 2, DistrictId = 3, Lat = 59.3080, Lng = 18.0710, AddressText = "Skanstull, Stockholm", CreatedByUserId = 4, AssignedToUserId = 3, Priority = IssuePriority.High, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Issue { Title = "Road surface deterioration on Fleminggatan", Description = "Extensive road surface wear between Scheelegatan and Pipersgatan. Multiple small potholes forming.", CategoryId = 5, StatusId = 5, DistrictId = 5, Lat = 59.3320, Lng = 18.0370, AddressText = "Fleminggatan 30, Stockholm", CreatedByUserId = 1, AssignedToUserId = 2, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-15) },
        };
        context.Issues.AddRange(issues);
        context.SaveChanges();

        // Comments
        var comments = new[]
        {
            new Comment { IssueId = 1, AuthorUserId = 1, Body = "Confirmed by field inspection. This needs urgent attention.", CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Comment { IssueId = 2, AuthorUserId = 2, Body = "Light fixture model identified: SL-2847. Replacement part ordered.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Comment { IssueId = 3, AuthorUserId = 3, Body = "Extra waste collection scheduled for this location.", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Comment { IssueId = 4, AuthorUserId = 2, Body = "Tree removal crew dispatched. Expected completion within 24 hours.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Comment { IssueId = 8, AuthorUserId = 2, Body = "Bench repaired and sanded. Safe for use.", CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new Comment { IssueId = 10, AuthorUserId = 1, Body = "Road resurfacing completed in full. Issue resolved and closed.", CreatedAt = DateTime.UtcNow.AddDays(-8) },
        };
        context.Comments.AddRange(comments);

        // Audit logs
        var audits = new[]
        {
            new AuditLog { IssueId = 2, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Reported\",\"to\":\"Confirmed\"}", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new AuditLog { IssueId = 2, ActorUserId = 1, Action = "Assigned", DetailsJson = "{\"assignedTo\":\"Erik Johansson\"}", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new AuditLog { IssueId = 3, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Reported\",\"to\":\"In Progress\"}", CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new AuditLog { IssueId = 4, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Reported\",\"to\":\"In Progress\"}", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new AuditLog { IssueId = 8, ActorUserId = 2, Action = "StatusChanged", DetailsJson = "{\"from\":\"In Progress\",\"to\":\"Resolved\"}", CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new AuditLog { IssueId = 10, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Resolved\",\"to\":\"Closed\"}", CreatedAt = DateTime.UtcNow.AddDays(-8) },
        };
        context.AuditLogs.AddRange(audits);

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
