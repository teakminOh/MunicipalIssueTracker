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

        // Districts (Námestovo and surrounding areas in Orava region)
        var districts = new[]
        {
            new District { DistrictId = 1, Name = "Centrum" },
            new District { DistrictId = 2, Name = "Sídlisko Brehy" },
            new District { DistrictId = 3, Name = "Sídlisko Kamence" },
            new District { DistrictId = 4, Name = "Stará Hora" },
            new District { DistrictId = 5, Name = "Okoličné" },
            new District { DistrictId = 6, Name = "Slanická Osada" },
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
            new User { UserId = 4, DisplayName = "Anna Kučerová", Email = "anna.kucerova@namestovo.sk", Role = UserRole.Viewer, PasswordHash = HashPassword("Viewer123!"), IsActive = true },
        };
        context.Users.AddRange(users);
        context.SaveChanges();

        // Issues — Námestovo-area coordinates for realism
        var issues = new[]
        {
            new Issue { Title = "Veľký výtlk na Hviezdoslavovej ulici", Description = "Hlboký výtlk blízko križovatky s Cyrila a Metoda, priemer cca 30cm. Riziko pre cyklistov.", CategoryId = 1, StatusId = 1, DistrictId = 1, Lat = 49.4075, Lng = 19.4855, AddressText = "Hviezdoslavova 12, Námestovo", CreatedByUserId = 4, Priority = IssuePriority.High, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new Issue { Title = "Nefunkčné pouličné osvetlenie na Hattalovej", Description = "Pouličné svetlo #NO-284 nefunguje 3 dni. Križovatka je v noci slabo osvetlená.", CategoryId = 2, StatusId = 2, DistrictId = 3, Lat = 49.4048, Lng = 19.4910, AddressText = "Hattalova 18, Námestovo", CreatedByUserId = 4, AssignedToUserId = 2, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Issue { Title = "Preplnený odpadkový kôš pri námestí", Description = "Verejný odpadkový kôš na južnom konci námestia je preplnený od pondelka.", CategoryId = 3, StatusId = 3, DistrictId = 1, Lat = 49.4082, Lng = 19.4838, AddressText = "Námestie A. Bernoláka, Námestovo", CreatedByUserId = 4, AssignedToUserId = 3, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Issue { Title = "Padnutý strom v parku pri priehrade", Description = "Veľký strom spadol cez hlavný chodník po búrke. Blokuje prístup pre chodcov a cyklistov.", CategoryId = 4, StatusId = 3, DistrictId = 6, Lat = 49.4120, Lng = 19.4670, AddressText = "Slanická Osada, Námestovo", CreatedByUserId = 1, AssignedToUserId = 2, Priority = IssuePriority.High, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Issue { Title = "Popraskané chodníky na Brehovej", Description = "Viaceré praskliny v chodníku pri autobusovej zastávke. Nebezpečenstvo zakopnutia pre starších ľudí.", CategoryId = 5, StatusId = 1, DistrictId = 2, Lat = 49.4055, Lng = 19.4790, AddressText = "Brehová 45, Námestovo", CreatedByUserId = 4, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Issue { Title = "Grafiti na stene školy na Kamencoch", Description = "Veľké grafiti nastriekané na južnej stene školskej budovy. Obsahuje nevhodné obrázky.", CategoryId = 6, StatusId = 2, DistrictId = 3, Lat = 49.4040, Lng = 19.4880, AddressText = "Sídlisko Kamence, Námestovo", CreatedByUserId = 4, AssignedToUserId = 3, Priority = IssuePriority.Low, CreatedAt = DateTime.UtcNow.AddDays(-6) },
            new Issue { Title = "Upchatý odtok na Rázusovej ulici", Description = "Kanalizačný odtok je úplne upchatý. Voda sa hromadí na ceste počas dažďa, spôsobuje dopravné problémy.", CategoryId = 7, StatusId = 1, DistrictId = 1, Lat = 49.4068, Lng = 19.4820, AddressText = "Rázusova 8, Námestovo", CreatedByUserId = 1, Priority = IssuePriority.Critical, CreatedAt = DateTime.UtcNow.AddHours(-6) },
            new Issue { Title = "Poškodená lavička v mestskom parku", Description = "Zlomené drevené laty na lavičke pri ihrisku. Ostré hrany nebezpečné pre deti.", CategoryId = 4, StatusId = 4, DistrictId = 4, Lat = 49.4090, Lng = 19.4900, AddressText = "Mestský park, Námestovo", CreatedByUserId = 4, AssignedToUserId = 2, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Issue { Title = "Nelegálne skládkovanie pri Brehoch", Description = "Nábytok a stavebný odpad vysypaný na chodníku. Čiastočne blokuje cestu pre chodcov.", CategoryId = 3, StatusId = 2, DistrictId = 2, Lat = 49.4045, Lng = 19.4770, AddressText = "Sídlisko Brehy, Námestovo", CreatedByUserId = 4, AssignedToUserId = 3, Priority = IssuePriority.High, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Issue { Title = "Poškodená cesta na Okružnej", Description = "Rozsiahle opotrebenie povrchu cesty medzi Okružnou a Hattalovou. Tvoria sa viaceré malé výtlky.", CategoryId = 5, StatusId = 5, DistrictId = 5, Lat = 49.4030, Lng = 19.4850, AddressText = "Okružná 30, Námestovo", CreatedByUserId = 1, AssignedToUserId = 2, Priority = IssuePriority.Medium, CreatedAt = DateTime.UtcNow.AddDays(-15) },
        };
        context.Issues.AddRange(issues);
        context.SaveChanges();

        // Comments
        var comments = new[]
        {
            new Comment { IssueId = 1, AuthorUserId = 1, Body = "Potvrdené terénnou obhliadkou. Vyžaduje si naliehavú opravu.", CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Comment { IssueId = 2, AuthorUserId = 2, Body = "Model svietidla identifikovaný: NO-284. Náhradný diel objednaný.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Comment { IssueId = 3, AuthorUserId = 3, Body = "Mimoriadny odvoz odpadu naplánovaný na túto lokalitu.", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Comment { IssueId = 4, AuthorUserId = 2, Body = "Tím na odstraňovanie stromov vyslaný. Predpokladané dokončenie do 24 hodín.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Comment { IssueId = 8, AuthorUserId = 2, Body = "Lavička opravená a obrúsená. Bezpečná na použitie.", CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new Comment { IssueId = 10, AuthorUserId = 1, Body = "Prefrézovanie cesty dokončené v plnom rozsahu. Problém vyriešený a uzavretý.", CreatedAt = DateTime.UtcNow.AddDays(-8) },
        };
        context.Comments.AddRange(comments);

        // Audit logs
        var audits = new[]
        {
            new AuditLog { IssueId = 2, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Nahlásené\",\"to\":\"Potvrdené\"}", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new AuditLog { IssueId = 2, ActorUserId = 1, Action = "Assigned", DetailsJson = "{\"assignedTo\":\"Ján Kováč\"}", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new AuditLog { IssueId = 3, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Nahlásené\",\"to\":\"V riešení\"}", CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new AuditLog { IssueId = 4, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Nahlásené\",\"to\":\"V riešení\"}", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new AuditLog { IssueId = 8, ActorUserId = 2, Action = "StatusChanged", DetailsJson = "{\"from\":\"V riešení\",\"to\":\"Vyriešené\"}", CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new AuditLog { IssueId = 10, ActorUserId = 1, Action = "StatusChanged", DetailsJson = "{\"from\":\"Vyriešené\",\"to\":\"Uzavreté\"}", CreatedAt = DateTime.UtcNow.AddDays(-8) },
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
