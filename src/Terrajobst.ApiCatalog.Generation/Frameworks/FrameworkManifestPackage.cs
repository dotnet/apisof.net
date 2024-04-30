using System.Text.Json.Serialization;

namespace Terrajobst.ApiCatalog;

public sealed class FrameworkManifestPackage
{
    public FrameworkManifestPackage(string id,
                                    string version,
                                    IReadOnlyList<string> references)
    {
        Id = id;
        Version = version;
        References = references.Order().ToArray();
    }

    public string Id { get; }

    public string Version { get; }

    [JsonIgnore]
    public IReadOnlyList<string> References { get; }
}