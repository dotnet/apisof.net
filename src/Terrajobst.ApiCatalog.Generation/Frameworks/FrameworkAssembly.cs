namespace Terrajobst.ApiCatalog;

public sealed class FrameworkAssembly
{
    public FrameworkAssembly(string path)
        : this(path, null, Array.Empty<string>())
    {
    }


    public FrameworkAssembly(string path, string? packName, IReadOnlyList<string> profiles)
    {
        ThrowIfNull(path);
        ThrowIfNull(profiles);

        Path = path;
        PackName = packName;
        Profiles = profiles;
    }

    public string Path { get; }
    public string? PackName { get; }
    public IReadOnlyList<string> Profiles { get; }
}