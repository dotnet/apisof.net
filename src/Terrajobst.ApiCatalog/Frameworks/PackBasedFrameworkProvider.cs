namespace Terrajobst.ApiCatalog;

public sealed class PackBasedFrameworkProvider : FrameworkProvider
{
    private readonly string _frameworksPath;

    public PackBasedFrameworkProvider(string frameworksPath)
    {
        _frameworksPath = frameworksPath;
    }

    public override IEnumerable<(string FrameworkName, string[] Paths)> Resolve()
    {
        var packIndexPath = Path.Combine(_frameworksPath, FrameworkManifest.FileName);
        if (!File.Exists(packIndexPath))
            yield break;

        var manifest = FrameworkManifest.Load(packIndexPath);
        var references = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifest.Frameworks)
        {
            references.Clear();

            foreach (var package in entry.Packages)
                references.UnionWith(package.References);

            var files = references.ToArray();
            yield return (entry.FrameworkName, files);
        }
    }
}