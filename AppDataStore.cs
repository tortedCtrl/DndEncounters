using System.Text.Json;
using System.Text.Json.Serialization;

namespace DndEncounters;

public sealed class AppDataStore
{
    private readonly string _dataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DndEncounters");
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ImageDirectory => Path.Combine(_dataDirectory, "images");

    public List<T> Load<T>(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
            return [];

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? [];
    }

    public void Save<T>(string fileName, List<T> items)
    {
        Directory.CreateDirectory(_dataDirectory);
        var path = Path.Combine(_dataDirectory, fileName);
        var temporaryPath = $"{path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(items, _jsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    public string ImportImage(string sourcePath)
    {
        Directory.CreateDirectory(ImageDirectory);
        var extension = Path.GetExtension(sourcePath);
        var destinationPath = Path.Combine(ImageDirectory, $"{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, destinationPath);
        return destinationPath;
    }
}
