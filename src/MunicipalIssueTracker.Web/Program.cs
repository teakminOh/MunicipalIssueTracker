using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Interfaces;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Infrastructure.Repositories;
using MunicipalIssueTracker.Infrastructure.Storage;
using MunicipalIssueTracker.Web.Components;
using MunicipalIssueTracker.Web.Middleware;
using MunicipalIssueTracker.Web.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// EF Core with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Application services
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<IssueService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IssueClassificationService>();
builder.Services.AddSingleton<IFileStorage>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var uploadPath = Path.Combine(env.ContentRootPath, "uploads");
    return new LocalFileStorage(uploadPath);
});

// Radzen
builder.Services.AddRadzenComponents();

// Rate limiting: protect login from brute-force
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Minimal API login/logout endpoints
app.MapPost("/api/auth/login", async (HttpContext ctx, AppDbContext db, LoginRequest request) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
    if (user == null || !SeedData.VerifyPassword(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role.ToString())
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { user.DisplayName, Role = user.Role.ToString() });
}).RequireRateLimiting("login");

app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

// Minimal API for external importers (e.g., WinForms CSV Importer)
app.MapPost("/api/issues/import", async (HttpContext ctx, AppDbContext db, ILogger<Program> logger, List<ImportIssueRequest> issues) =>
{
    if (issues == null || issues.Count == 0)
        return Results.BadRequest(new { error = "No issues provided." });

    if (issues.Count > 500)
        return Results.BadRequest(new { error = "Maximum 500 issues per import batch." });

    var imported = 0;
    var skipped = 0;
    foreach (var req in issues)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || req.Title.Length > 200)
        {
            skipped++;
            continue;
        }

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == req.Category);
        var district = await db.Districts.FirstOrDefaultAsync(d => d.Name == req.District);
        if (category == null || district == null) { skipped++; continue; }

        db.Issues.Add(new MunicipalIssueTracker.Domain.Entities.Issue
        {
            Title = req.Title.Trim(),
            Description = (req.Description ?? "").Trim(),
            CategoryId = category.CategoryId,
            StatusId = 1, // Submitted
            DistrictId = district.DistrictId,
            Lat = req.Lat,
            Lng = req.Lng,
            AddressText = (req.Address ?? "").Trim(),
            Priority = Enum.TryParse<MunicipalIssueTracker.Domain.Enums.IssuePriority>(req.Priority, true, out var p)
                ? p : MunicipalIssueTracker.Domain.Enums.IssuePriority.Medium,
            CreatedByUserId = 1, // Default admin user for imports
            CreatedAt = DateTime.UtcNow
        });
        imported++;
    }
    await db.SaveChangesAsync();
    logger.LogInformation("Import completed: {Imported} imported, {Skipped} skipped", imported, skipped);
    return Results.Ok(new { imported, skipped });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// File download endpoint — with resource-level access control for citizens
app.MapGet("/api/attachments/{id:int}", async (int id, HttpContext ctx, AppDbContext db, IFileStorage storage) =>
{
    var att = await db.Attachments.FindAsync(id);
    if (att == null) return Results.NotFound();

    // Citizens can only download attachments from their own issues
    var roleClaim = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
    if (roleClaim == "Citizen")
    {
        var userIdStr = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdStr != null)
        {
            var issue = await db.Issues.FindAsync(att.IssueId);
            if (issue == null || issue.CreatedByUserId.ToString() != userIdStr)
                return Results.Forbid();
        }
    }

    var stream = await storage.GetFileAsync(att.StoragePath);
    if (stream == null) return Results.NotFound();
    return Results.File(stream, att.ContentType, att.FileName);
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public record LoginRequest(string Email, string Password);
public record ImportIssueRequest(string Title, string? Description, string Category, string District, double Lat, double Lng, string? Address, string? Priority);
