using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MunicipalIssueTracker.Domain.Entities;
using MunicipalIssueTracker.Domain.Enums;
using MunicipalIssueTracker.Infrastructure.Data;
using MunicipalIssueTracker.Infrastructure.Repositories;
using MunicipalIssueTracker.Web.Services;

namespace MunicipalIssueTracker.Tests;

public class ClassificationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IssueClassificationService _classifier;

    public ClassificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        SeedCategories();
        var repo = new IssueRepository(_db);
        var issueService = new IssueService(repo, NullLogger<IssueService>.Instance);
        _classifier = new IssueClassificationService(issueService);
    }

    private void SeedCategories()
    {
        _db.Categories.AddRange(
            new Category { CategoryId = 1, Name = "Pothole", DefaultPriority = IssuePriority.High },
            new Category { CategoryId = 2, Name = "Broken Street Light", DefaultPriority = IssuePriority.Medium },
            new Category { CategoryId = 3, Name = "Waste / Littering", DefaultPriority = IssuePriority.Medium },
            new Category { CategoryId = 4, Name = "Greenery / Parks", DefaultPriority = IssuePriority.Low },
            new Category { CategoryId = 5, Name = "Road Damage", DefaultPriority = IssuePriority.High },
            new Category { CategoryId = 6, Name = "Graffiti / Vandalism", DefaultPriority = IssuePriority.Low },
            new Category { CategoryId = 7, Name = "Water / Drainage", DefaultPriority = IssuePriority.Medium },
            new Category { CategoryId = 8, Name = "Other", DefaultPriority = IssuePriority.Medium }
        );
        _db.Statuses.Add(new Status { StatusId = 1, Name = "Submitted", SortOrder = 1 });
        _db.Districts.Add(new District { DistrictId = 1, Name = "Centrum" });
        _db.SaveChanges();
    }

    // --- ClassifyCategory tests ---

    [Theory]
    [InlineData("Pothole on main street", "", 1)]
    [InlineData("Výtlk na ceste", "", 1)]
    [InlineData("Jama pred školou", "", 1)]
    public void ClassifyCategory_PotholeKeywords_ReturnsCategoryOne(string title, string desc, int expected)
    {
        Assert.Equal(expected, _classifier.ClassifyCategory(title, desc));
    }

    [Theory]
    [InlineData("Street light broken", "", 2)]
    [InlineData("Lampa nesvieti", "", 2)]
    [InlineData("Osvetlenie nefunguje", "", 2)]
    public void ClassifyCategory_StreetLightKeywords_ReturnsCategoryTwo(string title, string desc, int expected)
    {
        Assert.Equal(expected, _classifier.ClassifyCategory(title, desc));
    }

    [Theory]
    [InlineData("Waste dumped near park", "", 3)]
    [InlineData("Odpad na ulici", "", 3)]
    [InlineData("Kontajner preplnený", "", 3)]
    public void ClassifyCategory_WasteKeywords_ReturnsCategoryThree(string title, string desc, int expected)
    {
        Assert.Equal(expected, _classifier.ClassifyCategory(title, desc));
    }

    [Theory]
    [InlineData("Tree fell on sidewalk", "", 4)]
    [InlineData("Park lavička zlomená", "", 4)]
    [InlineData("Bench needs repair", "", 4)]
    public void ClassifyCategory_GreeneryKeywords_ReturnsCategoryFour(string title, string desc, int expected)
    {
        Assert.Equal(expected, _classifier.ClassifyCategory(title, desc));
    }

    [Fact]
    public void ClassifyCategory_NoMatchingKeywords_ReturnsFallbackEight()
    {
        Assert.Equal(8, _classifier.ClassifyCategory("Something completely unrelated", "No keywords here"));
    }

    [Fact]
    public void ClassifyCategory_MultipleCategories_ReturnsHighestScoring()
    {
        // "pothole" (1 match for cat 1), "road sidewalk pavement" (3 matches for cat 5)
        var result = _classifier.ClassifyCategory("pothole", "road sidewalk pavement damage");
        Assert.Equal(5, result); // Road Damage has more keyword matches
    }

    [Fact]
    public void ClassifyCategory_CaseInsensitive()
    {
        Assert.Equal(1, _classifier.ClassifyCategory("POTHOLE", ""));
        Assert.Equal(6, _classifier.ClassifyCategory("GRAFFITI on wall", ""));
    }

    [Fact]
    public void ClassifyCategory_KeywordsInDescription()
    {
        Assert.Equal(7, _classifier.ClassifyCategory("Problem reported", "water flooding drain"));
    }

    // --- ClassifyPriority tests ---

    [Fact]
    public async Task ClassifyPriority_NoUrgencyKeywords_ReturnsBasePriority()
    {
        var priority = await _classifier.ClassifyPriorityAsync("Normal pothole", "On the road", 1);
        Assert.Equal(IssuePriority.High, priority); // Category 1 default is High
    }

    [Fact]
    public async Task ClassifyPriority_UrgentKeyword_BumpsUpOneLevel()
    {
        var priority = await _classifier.ClassifyPriorityAsync("Dangerous pothole", "danger to traffic", 2);
        // Category 2 default = Medium, danger bumps to High
        Assert.Equal(IssuePriority.High, priority);
    }

    [Fact]
    public async Task ClassifyPriority_UrgentKeyword_DoesNotExceedCritical()
    {
        // Category 1 default = High, urgent bumps to Critical (max)
        var priority = await _classifier.ClassifyPriorityAsync("Critical pothole", "critical danger", 1);
        Assert.Equal(IssuePriority.Critical, priority);
    }

    [Fact]
    public async Task ClassifyPriority_LowKeyword_LowersOneLevel()
    {
        var priority = await _classifier.ClassifyPriorityAsync("Minor issue", "cosmetic only", 2);
        // Category 2 default = Medium, "minor"/"cosmetic" lowers to Low
        Assert.Equal(IssuePriority.Low, priority);
    }

    [Fact]
    public async Task ClassifyPriority_LowKeyword_DoesNotGoBelowLow()
    {
        var priority = await _classifier.ClassifyPriorityAsync("Minor cosmetic", "", 4);
        // Category 4 default = Low, can't go lower
        Assert.Equal(IssuePriority.Low, priority);
    }

    [Fact]
    public async Task ClassifyPriority_UnknownCategory_FallsBackToMedium()
    {
        var priority = await _classifier.ClassifyPriorityAsync("Some issue", "", 999);
        Assert.Equal(IssuePriority.Medium, priority);
    }

    // --- ClassifyDistrict tests ---

    [Fact]
    public void ClassifyDistrict_ExactCenter_ReturnsCorrectDistrict()
    {
        // Exact center of "Stred" (district 1)
        Assert.Equal(1, _classifier.ClassifyDistrict(49.4075, 19.4838));
    }

    [Fact]
    public void ClassifyDistrict_NearBrehy_ReturnsDistrictTwo()
    {
        Assert.Equal(2, _classifier.ClassifyDistrict(49.4050, 19.4770));
    }

    [Fact]
    public void ClassifyDistrict_NearSlanickaOsada_ReturnsDistrictNine()
    {
        Assert.Equal(9, _classifier.ClassifyDistrict(49.4120, 19.4670));
    }

    [Fact]
    public void ClassifyDistrict_FarAwayPoint_ReturnsNearestDistrict()
    {
        // A point far north should still return the nearest district
        var result = _classifier.ClassifyDistrict(49.50, 19.48);
        Assert.InRange(result, 1, 9);
    }

    [Fact]
    public void ClassifyDistrict_AddressContainsDistrictName_OverridesCoordinates()
    {
        // Coordinates are nearest to Predmostie (district 4), but address says Čerchle (district 3)
        Assert.Equal(3, _classifier.ClassifyDistrict(49.4095, 19.4780, "Ulica 123, Čerchle"));
    }

    [Fact]
    public void ClassifyDistrict_AddressContainsDistrictName_CaseInsensitive()
    {
        Assert.Equal(3, _classifier.ClassifyDistrict(49.4095, 19.4780, "ulica na ČERCHLE 5"));
    }

    [Fact]
    public void ClassifyDistrict_AddressWithNoDistrictName_FallsBackToCoordinates()
    {
        // Address has no district name, should use coordinate-based fallback
        Assert.Equal(4, _classifier.ClassifyDistrict(49.4095, 19.4780, "Some random address 42"));
    }

    [Fact]
    public void ClassifyDistrict_NullAddress_FallsBackToCoordinates()
    {
        Assert.Equal(4, _classifier.ClassifyDistrict(49.4095, 19.4780, null));
    }

    [Fact]
    public void ClassifyDistrict_AddressSlanicaWithoutSuffix_MatchesNearestSlanicaDistrict()
    {
        // Address says "Slanica" without I/II — should match one of the Slanica districts
        // using coordinates to break the tie. Coordinates are nearest to Slanica I (district 7).
        Assert.Equal(7, _classifier.ClassifyDistrict(49.4130, 19.4720, "Slanica, ulica 5"));
    }

    [Fact]
    public void ClassifyDistrict_AddressSlanicaII_ExactMatch()
    {
        // Full name "Slanica II" appears in address — exact match to district 8
        Assert.Equal(8, _classifier.ClassifyDistrict(49.4075, 19.4838, "Slanica II, hlavná 10"));
    }

    [Fact]
    public void ClassifyDistrict_AddressContainsStredneDoesNotMatchStred()
    {
        // "Stredné Slovensko" should NOT match district "Stred" — word boundary check
        // Address explicitly says "Brehy", so district 2 is correct.
        Assert.Equal(2, _classifier.ClassifyDistrict(49.4050, 19.4770,
            "Severná, Námestovo - Brehy, Námestovo, okres Námestovo, Žilinský kraj, Stredné Slovensko, 029 01, Slovensko"));
    }

    public void Dispose() => _db.Dispose();
}
