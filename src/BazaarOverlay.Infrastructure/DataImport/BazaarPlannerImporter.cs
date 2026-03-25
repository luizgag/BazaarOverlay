using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        _logger.LogInformation("Fetching items from {Url}", $"{BaseUrl}items.js");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}items.js");
        if (js.Length == 0)
        {
            _logger.LogWarning("BazaarPlanner items.js returned empty response");
            return [];
        }
        _logger.LogDebug("items.js response: {Length} bytes, preview: {Preview}",
            js.Length, js[..Math.Min(200, js.Length)]);
        var items = ParseJsExport<BazaarPlannerItem>(js, _logger);
        if (items.Count == 0)
            _logger.LogWarning("BazaarPlanner items.js returned content ({Length} bytes) but parsed to 0 items",
                js.Length);
        _logger.LogInformation("Fetched {Count} items from BazaarPlanner", items.Count);
        return items;
    }

    public async Task<List<BazaarPlannerMonster>> FetchMonstersAsync()
    {
        _logger.LogInformation("Fetching monsters from {Url}", $"{BaseUrl}monsters.js");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}monsters.js");
        if (js.Length == 0)
        {
            _logger.LogWarning("BazaarPlanner monsters.js returned empty response");
            return [];
        }
        _logger.LogDebug("monsters.js response: {Length} bytes, preview: {Preview}",
            js.Length, js[..Math.Min(200, js.Length)]);
        var monsters = ParseJsExport<BazaarPlannerMonster>(js, _logger);
        if (monsters.Count == 0)
            _logger.LogWarning("BazaarPlanner monsters.js returned content ({Length} bytes) but parsed to 0 monsters",
                js.Length);
        _logger.LogInformation("Fetched {Count} monsters from BazaarPlanner", monsters.Count);
        return monsters;
    }

    public async Task<List<BazaarPlannerSkill>> FetchSkillsAsync()
    {
        _logger.LogInformation("Fetching skills from {Url}", $"{BaseUrl}skills.js");
        var js = await _httpClient.GetStringAsync($"{BaseUrl}skills.js");
        if (js.Length == 0)
        {
            _logger.LogWarning("BazaarPlanner skills.js returned empty response");
            return [];
        }
        _logger.LogDebug("skills.js response: {Length} bytes, preview: {Preview}",
            js.Length, js[..Math.Min(200, js.Length)]);
        var skills = ParseJsExport<BazaarPlannerSkill>(js, _logger);
        if (skills.Count == 0)
            _logger.LogWarning("BazaarPlanner skills.js returned content ({Length} bytes) but parsed to 0 skills",
                js.Length);
        _logger.LogInformation("Fetched {Count} skills from BazaarPlanner", skills.Count);
        return skills;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new NumberToStringConverter() }
    };

    private class NumberToStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.Null => null,
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public static List<T> ParseJsExport<T>(string jsContent, ILogger? logger = null)
    {
        // Try array format: "export const x = [...]"
        var arrayMatch = ArrayPattern().Match(jsContent);
        if (arrayMatch.Success)
        {
            logger?.LogDebug("Matched array export pattern for {Type}", typeof(T).Name);
            var json = TrailingCommaPattern().Replace(arrayMatch.Groups[1].Value, "$1");
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to deserialize array JSON for {Type}. JSON preview: {Preview}",
                    typeof(T).Name, json[..Math.Min(200, json.Length)]);
                return [];
            }
        }

        // Try object format: "export const x = {...}"
        var objectMatch = ObjectPattern().Match(jsContent);
        if (objectMatch.Success)
        {
            logger?.LogDebug("Matched object export pattern for {Type}", typeof(T).Name);
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
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                logger?.LogWarning(ex, "Failed to deserialize object JSON for {Type}. JSON preview: {Preview}",
                    typeof(T).Name, json[..Math.Min(200, json.Length)]);
                return [];
            }
        }

        logger?.LogWarning("No export pattern matched in content ({Length} chars). Preview: {Preview}",
            jsContent.Length, jsContent[..Math.Min(200, jsContent.Length)]);
        return [];
    }

    [GeneratedRegex(@"=\s*(\[.*\])\s*;?\s*$", RegexOptions.Singleline)]
    private static partial Regex ArrayPattern();

    [GeneratedRegex(@"=\s*(\{.*\})\s*;?\s*$", RegexOptions.Singleline)]
    private static partial Regex ObjectPattern();

    [GeneratedRegex(@",(\s*[\]\}])")]
    private static partial Regex TrailingCommaPattern();
}
