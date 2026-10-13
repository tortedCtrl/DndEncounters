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
        var legacyPath = Path.Combine(DataDirectory, fileName);
        var folder = Path.Combine(DataDirectory, FolderFor(fileName));
        if (Directory.Exists(folder))
        {
            var result = new List<T>();
            foreach (var path in Directory.EnumerateFiles(folder, "*.json").OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase))
            {
                try
                {
                    var item = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _jsonOptions);
                    if (item is not null) result.Add(item);
                }
                catch (JsonException) { /* Skip malformed individual records, don't prevent startup. */ }
            }
            if (result.Count > 0 || !File.Exists(legacyPath)) return result;
        }
        if (!File.Exists(legacyPath)) return [];
        // Import legacy collection files on first read; the next save migrates to individual files.
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(legacyPath), _jsonOptions) ?? [];
    }

    public void Save<T>(string fileName, List<T> items)
    {
        Directory.CreateDirectory(DataDirectory);
        var folder = Path.Combine(DataDirectory, FolderFor(fileName));
        Directory.CreateDirectory(folder);
        var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var name = GetItemName(item) ?? "Без имени";
            var safeName = SafeFileName(name);
            var path = Path.Combine(folder, safeName + ".json");
            if (!desired.Add(path))
            {
                var id = item?.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
                path = Path.Combine(folder, safeName + "_" + (string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id) + ".json");
                desired.Add(path);
            }
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(item, _jsonOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        // Remove records deleted in the UI, but leave non-JSON files untouched.
        foreach (var existing in Directory.EnumerateFiles(folder, "*.json"))
            if (!desired.Contains(existing)) File.Delete(existing);
    }

    private static string FolderFor(string fileName) => Path.GetFileNameWithoutExtension(fileName) switch
    {
        "monsters" => "monsters",
        "heroes" => "heroes",
        "encounters" => "encounters",
        "encounter_editor_heroes" => "encounter_editor_heroes",
        var other => other
    };

    private static string? GetItemName<T>(T item)
    {
        if (item is Guid guid) return guid.ToString();
        var property = item?.GetType().GetProperty("Name");
        return property?.GetValue(item) as string
            ?? item?.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var safe = new string(name.Trim().Select(c => invalid.Contains(c) || c is '/' or '\\' ? '_' : c).ToArray());
        safe = safe.Trim('.', ' ');
        return string.IsNullOrWhiteSpace(safe) ? "Без имени" : safe;
    }

    // Store the original path, never copy image bytes. A missing source is handled by the UI.
    public string ImportImage(string sourcePath) => Path.GetFullPath(sourcePath);
}
