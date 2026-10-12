using System.Text.Json;
using System.Text.Json.Serialization;

namespace DndEncounters;

public sealed class AppDataStore
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DndEncounters");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string DataDirectory { get; private set; } = Path.Combine(AppContext.BaseDirectory, "Data");
    public string ImageDirectory => DataDirectory;
    public string Theme { get; private set; } = "light";

    public AppDataStore()
    {
        try
        {
            // Keep existing installations' data when upgrading from the previous storage location.
            var legacyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DndEncounters");
            if (!File.Exists(SettingsPath) && File.Exists(Path.Combine(legacyDirectory, "monsters.json")))
                DataDirectory = legacyDirectory;
            if (File.Exists(SettingsPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
                if (doc.RootElement.TryGetProperty("dataDirectory", out var dir) &&
                    dir.GetString() is { Length: > 0 } value) DataDirectory = value;
                if (doc.RootElement.TryGetProperty("theme", out var theme) && theme.GetString() is { } t)
                    Theme = t;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }

    public void Configure(string? dataDirectory = null, string? theme = null)
    {
        if (!string.IsNullOrWhiteSpace(dataDirectory)) DataDirectory = Path.GetFullPath(dataDirectory);
        if (!string.IsNullOrWhiteSpace(theme)) Theme = theme;
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new { dataDirectory = DataDirectory, theme = Theme }, _jsonOptions));
        Directory.CreateDirectory(DataDirectory);
    }

    public List<T> Load<T>(string fileName)
    {
        var path = Path.Combine(DataDirectory, fileName);
        if (!File.Exists(path)) return [];
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? [];
    }

    public void Save<T>(string fileName, List<T> items)
    {
        Directory.CreateDirectory(DataDirectory);
        var path = Path.Combine(DataDirectory, fileName);
        var temporaryPath = $"{path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(items, _jsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    // Store the original path, never copy image bytes. A missing source is handled by the UI.
    public string ImportImage(string sourcePath) => Path.GetFullPath(sourcePath);
}
