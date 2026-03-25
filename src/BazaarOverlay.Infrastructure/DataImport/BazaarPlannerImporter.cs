using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.DataImport;

public record BazaarPlannerItem(
    string Name, string? Size, string? Cooldown, string? StartingTier,
    List<string>? Tags, List<string>? Heroes, string? InternalName);

public record BazaarPlannerMonster(
    string Name, string? Tier, int? Health, int? Day,
    List<string>? Items, List<string>? Skills);

public record BazaarPlannerSkill(
    string Name, string? StartingTier, List<string>? Tags, List<string>? Heroes);

public partial class BazaarPlannerImporter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BazaarPlannerImporter> _logger;
    private const string BaseUrl = "https://raw.githubusercontent.com/oceanseth/BazaarPlanner/main/";

    public BazaarPlannerImporter(HttpClient httpClient, ILogger<BazaarPlannerImporter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<BazaarPlannerItem>> FetchItemsAsync()
    {
        _logger.LogInformation("Fetching items from BazaarPlanner...");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}items.js");
        var items = ParseJsArray<BazaarPlannerItem>(js);
        if (items.Count == 0 && js.Length > 0)
            _logger.LogWarning("BazaarPlanner items.js returned content but parsed to 0 items");
        _logger.LogInformation("Fetched {Count} items from BazaarPlanner", items.Count);
        return items;
    }

    public async Task<List<BazaarPlannerMonster>> FetchMonstersAsync()
    {
        _logger.LogInformation("Fetching monsters from BazaarPlanner...");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}monsters.js");
        var monsters = ParseJsArray<BazaarPlannerMonster>(js);
        if (monsters.Count == 0 && js.Length > 0)
            _logger.LogWarning("BazaarPlanner monsters.js returned content but parsed to 0 monsters");
        _logger.LogInformation("Fetched {Count} monsters from BazaarPlanner", monsters.Count);
        return monsters;
    }

    public async Task<List<BazaarPlannerSkill>> FetchSkillsAsync()
    {
        _logger.LogInformation("Fetching skills from BazaarPlanner...");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}skills.js");
        var skills = ParseJsArray<BazaarPlannerSkill>(js);
        if (skills.Count == 0 && js.Length > 0)
            _logger.LogWarning("BazaarPlanner skills.js returned content but parsed to 0 skills");
        _logger.LogInformation("Fetched {Count} skills from BazaarPlanner", skills.Count);
        return skills;
    }

    public static List<T> ParseJsArray<T>(string jsContent)
    {
        // Extract JSON array from JS export: "export const x = [...]" or "const x = [...]"
        var match = ArrayPattern().Match(jsContent);
        if (!match.Success)
            return new List<T>();

        var json = match.Groups[1].Value;

        // Fix JS-specific syntax: trailing commas, single quotes, unquoted keys
        json = TrailingCommaPattern().Replace(json, "$1");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    [GeneratedRegex(@"=\s*(\[.*\])\s*;?\s*$", RegexOptions.Singleline)]
    private static partial Regex ArrayPattern();

    [GeneratedRegex(@",(\s*[\]\}])")]
    private static partial Regex TrailingCommaPattern();
}
