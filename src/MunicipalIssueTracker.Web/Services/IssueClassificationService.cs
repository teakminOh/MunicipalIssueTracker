using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MunicipalIssueTracker.Web.Services;

public class IssueClassificationService
{
    private static readonly Dictionary<string, int> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // CategoryId 1: Pothole
        ["výtlk"] = 1, ["pothole"] = 1, ["jama"] = 1, ["diera v ceste"] = 1,

        // CategoryId 2: Broken Street Light
        ["osvetlenie"] = 2, ["svetlo"] = 2, ["lampa"] = 2, ["street light"] = 2, ["pouličné"] = 2, ["lightbulb"] = 2,

        // CategoryId 3: Waste / Littering
        ["odpad"] = 3, ["kôš"] = 3, ["smetiar"] = 3, ["skládka"] = 3, ["smeti"] = 3, ["waste"] = 3,
        ["litter"] = 3, ["dumping"] = 3, ["kontajner"] = 3,

        // CategoryId 4: Greenery / Parks
        ["strom"] = 4, ["park"] = 4, ["zeleň"] = 4, ["lavička"] = 4, ["tráva"] = 4,
        ["ihrisko"] = 4, ["tree"] = 4, ["bench"] = 4, ["greenery"] = 4,

        // CategoryId 5: Road Damage
        ["cesta"] = 5, ["chodník"] = 5, ["asfalt"] = 5, ["prasklina"] = 5,
        ["road"] = 5, ["sidewalk"] = 5, ["pavement"] = 5,

        // CategoryId 6: Graffiti / Vandalism
        ["grafiti"] = 6, ["graffiti"] = 6, ["vandal"] = 6, ["nastriekan"] = 6, ["sprejer"] = 6,

        // CategoryId 7: Water / Drainage
        ["odtok"] = 7, ["kanál"] = 7, ["voda"] = 7, ["drenáž"] = 7, ["drain"] = 7,
        ["záplav"] = 7, ["potrubie"] = 7, ["water"] = 7, ["flood"] = 7,
    };

    // Approximate center coordinates for each district
    private static readonly (int DistrictId, string Name, double Lat, double Lng)[] DistrictCenters =
    [
        (1, "Stred",              49.4075, 19.4838),
        (2, "Brehy",              49.4050, 19.4770),
        (3, "Čerchle",            49.4044, 19.4895),
        (4, "Predmostie",         49.4095, 19.4780),
        (5, "Vojenské",           49.4030, 19.4850),
        (6, "Priemyselná zóna",   49.4010, 19.4920),
        (7, "Slanica I",          49.4130, 19.4720),
        (8, "Slanica II",         49.4140, 19.4690),
        (9, "Slanická Osada",     49.4120, 19.4670),
    ];

    private readonly IssueService _issueService;

    public IssueClassificationService(IssueService issueService)
    {
        _issueService = issueService;
    }

    /// <summary>
    /// Classifies category based on keywords in title and description.
    /// Returns the matching CategoryId, or falls back to "Other" (8).
    /// </summary>
    public int ClassifyCategory(string title, string description)
    {
        var text = $"{title} {description}";

        // Score each category by keyword matches
        var scores = new Dictionary<int, int>();
        foreach (var (keyword, categoryId) in CategoryKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                scores.TryGetValue(categoryId, out var count);
                scores[categoryId] = count + 1;
            }
        }

        if (scores.Count > 0)
            return scores.OrderByDescending(kvp => kvp.Value).First().Key;

        return 8; // "Other" fallback
    }

    /// <summary>
    /// Determines priority from the category's default priority,
    /// boosted by urgency keywords in the text.
    /// </summary>
    public async Task<IssuePriority> ClassifyPriorityAsync(string title, string description, int categoryId)
    {
        var categories = await _issueService.GetCategoriesAsync();
        var category = categories.FirstOrDefault(c => c.CategoryId == categoryId);
        var basePriority = category?.DefaultPriority ?? IssuePriority.Medium;

        var text = $"{title} {description}";
        var urgentKeywords = new[] { "nebezpeč", "danger", "urgent", "naliehav", "kritick", "critical", "blokuje", "block", "okamžit", "immediate", "ohrozu", "hazard" };
        var lowKeywords = new[] { "malý", "minor", "kozmetick", "cosmetic", "estetick" };

        bool hasUrgent = urgentKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        bool hasLow = lowKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (hasUrgent && basePriority < IssuePriority.Critical)
            return basePriority + 1; // Bump up one level

        if (hasLow && basePriority > IssuePriority.Low)
            return basePriority - 1; // Lower one level

        return basePriority;
    }

    /// <summary>
    /// Finds the nearest district based on coordinates.
    /// </summary>
    public int ClassifyDistrict(double lat, double lng)
    {
        var nearest = DistrictCenters
            .OrderBy(d => Math.Pow(d.Lat - lat, 2) + Math.Pow(d.Lng - lng, 2))
            .First();

        return nearest.DistrictId;
    }
}
