using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.DataImport;

public record BazaarPlannerItem(
    string Name, int? Tier, List<string>? Tags,
    string? Cooldown, string? Ammo);

public record BazaarPlannerMonsterEntry(string Name, int? Tier);

public record BazaarPlannerMonster(
    string Name, int? Day, int? Health,
    List<BazaarPlannerMonsterEntry>? Items,
    List<BazaarPlannerMonsterEntry>? Skills);

public record BazaarPlannerSkill(
    string Name, int? Tier, List<string>? Tags);

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
        var items = ParseJsExport<BazaarPlannerItem>(js);
        if (items.Count == 0 && js.Length > 0)
            _logger.LogWarning("BazaarPlanner items.js returned content but parsed to 0 items");
        _logger.LogInformation("Fetched {Count} items from BazaarPlanner", items.Count);
        return items;
    }

    public async Task<List<BazaarPlannerMonster>> FetchMonstersAsync()
    {
        _logger.LogInformation("Fetching monsters from BazaarPlanner...");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}monsters.js");
        var monsters = ParseJsExport<BazaarPlannerMonster>(js);
        if (monsters.Count == 0 && js.Length > 0)
            _logger.LogWarning("BazaarPlanner monsters.js returned content but parsed to 0 monsters");
        _logger.LogInformation("Fetched {Count} monsters from BazaarPlanner", monsters.Count);
        return monsters;
    }

    public async Task<List<BazaarPlannerSkill>> FetchSkillsAsync()
    {
        _logger.LogInformation("Fetching skills from BazaarPlanner...");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}skills.js");
        var skills = ParseJsExport<BazaarPlannerSkill>(js);
        if (skills.Count == 0 && js.Length > 0)
            _logger.LogWarning("BazaarPlanner skills.js returned content but parsed to 0 skills");
        _logger.LogInformation("Fetched {Count} skills from BazaarPlanner", skills.Count);
        return skills;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public static List<T> ParseJsExport<T>(string jsContent)
    {
        // Try array format: "export const x = [...]"
        var arrayMatch = ArrayPattern().Match(jsContent);
        if (arrayMatch.Success)
        {
            var json = TrailingCommaPattern().Replace(arrayMatch.Groups[1].Value, "$1");
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }

        // Try object format: "export const x = {...}"
        var objectMatch = ObjectPattern().Match(jsContent);
        if (objectMatch.Success)
        {
            var json = TrailingCommaPattern().Replace(objectMatch.Groups[1].Value, "$1");
            try
            {
                using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
                var results = new List<T>();
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    // Inject the key as "name" if the value object doesn't have one
                    var valueJson = property.Value.GetRawText();
                    if (property.Value.ValueKind == JsonValueKind.Object
                        && !property.Value.TryGetProperty("name", out _))
                    {
                        // Insert "name":"key" at the start of the object
                        valueJson = $"{{\"name\":{JsonSerializer.Serialize(property.Name)},{valueJson[1..]}";
                    }

                    var item = JsonSerializer.Deserialize<T>(valueJson, JsonOptions);
                    if (item is not null)
                        results.Add(item);
                }
                return results;
            }
            catch
            {
                return [];
            }
        }

        return [];
    }

    [GeneratedRegex(@"=\s*(\[.*\])\s*;?\s*$", RegexOptions.Singleline)]
    private static partial Regex ArrayPattern();

    [GeneratedRegex(@"=\s*(\{.*\})\s*;?\s*$", RegexOptions.Singleline)]
    private static partial Regex ObjectPattern();

    [GeneratedRegex(@",(\s*[\]\}])")]
    private static partial Regex TrailingCommaPattern();
}
