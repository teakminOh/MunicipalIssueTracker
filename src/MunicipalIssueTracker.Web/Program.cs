using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MunicipalIssueTracker.Domain.Interfaces;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Infrastructure.Storage;
using MunicipalIssueTracker.Web.Components;
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
builder.Services.AddScoped<IssueService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddSingleton<IFileStorage>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var uploadPath = Path.Combine(env.ContentRootPath, "uploads");
    return new LocalFileStorage(uploadPath);
});

// Radzen
builder.Services.AddRadzenComponents();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
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
});

app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});

// Minimal API for external importers (e.g., WinForms CSV Importer)
app.MapPost("/api/issues/import", async (AppDbContext db, List<ImportIssueRequest> issues) =>
{
    var imported = 0;
    foreach (var req in issues)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == req.Category);
        var district = await db.Districts.FirstOrDefaultAsync(d => d.Name == req.District);
        if (category == null || district == null) continue;

        db.Issues.Add(new MunicipalIssueTracker.Domain.Entities.Issue
        {
            Title = req.Title,
            Description = req.Description ?? "",
            CategoryId = category.CategoryId,
            StatusId = 1, // Reported
            DistrictId = district.DistrictId,
            Lat = req.Lat,
            Lng = req.Lng,
            AddressText = req.Address ?? "",
            Priority = Enum.TryParse<MunicipalIssueTracker.Domain.Enums.IssuePriority>(req.Priority, true, out var p)
                ? p : MunicipalIssueTracker.Domain.Enums.IssuePriority.Medium,
            CreatedByUserId = 1, // Default admin user for imports
            CreatedAt = DateTime.UtcNow
        });
        imported++;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { imported });
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public record LoginRequest(string Email, string Password);
public record ImportIssueRequest(string Title, string? Description, string Category, string District, double Lat, double Lng, string? Address, string? Priority);
