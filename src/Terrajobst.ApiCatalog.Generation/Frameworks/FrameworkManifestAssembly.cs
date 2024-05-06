using System.Text.Json.Serialization;

namespace Terrajobst.ApiCatalog;

public sealed class FrameworkManifestAssembly
{
    public FrameworkManifestAssembly(string path)
        : this(path, Array.Empty<string>())
    {
    }

    [JsonConstructor]
    public FrameworkManifestAssembly(string path,
                                     IReadOnlyList<string> profiles)
    {
        Path = path;
        Profiles = profiles;
    }

    public string Path { get; }

    public IReadOnlyList<string> Profiles { get; }
}