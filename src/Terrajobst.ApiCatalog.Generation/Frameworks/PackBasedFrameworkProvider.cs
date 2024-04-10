using NuGet.Frameworks;

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

        var entryByFx = manifest.Frameworks.ToDictionary(fx => NuGetFramework.Parse(fx.FrameworkName));

        foreach (var entry in manifest.Frameworks)
        {
            references.Clear();

            foreach (var package in entry.Packages)
                references.UnionWith(package.References);

            // For frameworks with a platform, such as `net5.0-windows`, we also want to add in all references
            // from the base framework, such as `net5.0`.

            var fx = NuGetFramework.Parse(entry.FrameworkName);
            var baseFx = GetBaseFramework(fx);
            if (baseFx is not null)
            {
                var baseEntry = entryByFx[baseFx];
                foreach (var package in baseEntry.Packages)
                    references.UnionWith(package.References);
            }

            var files = references.ToArray();
            yield return (entry.FrameworkName, files);
        }

        static NuGetFramework? GetBaseFramework(NuGetFramework fx)
        {
            var hasPlatform = fx.HasPlatform;
            var hasNetCoreApp3Profile = string.Equals(fx.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
                                        fx.Version.Major == 3 && fx.HasProfile;

            if (hasPlatform || hasNetCoreApp3Profile)
                return new NuGetFramework(fx.Framework, fx.Version);

            return null;
        }
    }
}