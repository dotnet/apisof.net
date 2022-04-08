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
        foreach (var directory in Directory.GetDirectories(_frameworksPath))
        {
            var packIndexPath = Path.Combine(directory, FrameworkPackIndex.FileName);
            if (!File.Exists(packIndexPath))
                continue;

            var entries = FrameworkPackIndex.Load(packIndexPath);

            foreach (var frameworkGroup in entries.GroupBy(e => e.FrameworkName))
            {
                var framework = frameworkGroup.Key;
                var files = frameworkGroup.SelectMany(g => g.AssemblyPaths)
                    .Select(p => Path.GetFullPath(Path.Combine(directory, p)))
                    .Distinct()
                    .ToArray();
                yield return (framework, files);
            }
        }
    }
}