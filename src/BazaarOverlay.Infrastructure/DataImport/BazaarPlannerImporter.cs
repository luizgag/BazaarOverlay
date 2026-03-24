using System.Text.Json;
using System.Text.RegularExpressions;

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
    private const string BaseUrl = "https://raw.githubusercontent.com/oceanseth/BazaarPlanner/main/";

    public BazaarPlannerImporter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BazaarPlannerItem>> FetchItemsAsync()
    {
        var js = await _httpClient.GetStringAsync($"{BaseUrl}items.js");
        return ParseJsArray<BazaarPlannerItem>(js);
    }

    public async Task<List<BazaarPlannerMonster>> FetchMonstersAsync()
    {
        var js = await _httpClient.GetStringAsync($"{BaseUrl}monsters.js");
        return ParseJsArray<BazaarPlannerMonster>(js);
    }

    public async Task<List<BazaarPlannerSkill>> FetchSkillsAsync()
    {
        var js = await _httpClient.GetStringAsync($"{BaseUrl}skills.js");
        return ParseJsArray<BazaarPlannerSkill>(js);
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
