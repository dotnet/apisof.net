using System.Text.Json;

namespace Terrajobst.ApiCatalog;

public sealed class FrameworkManifest
{
    public static string FileName => "frameworkManifest.json";

    public FrameworkManifest(IReadOnlyList<FrameworkManifestEntry> frameworks)
    {
        Frameworks = frameworks.OrderBy(f => f.FrameworkName).ToArray();
    }

    public IReadOnlyList<FrameworkManifestEntry> Frameworks { get; }

    public void Save(string path)
    {
        var settings = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, this, settings);
    }

    public static FrameworkManifest Load(string path)
    {
        var settings = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<FrameworkManifest>(stream, settings);
    }
}