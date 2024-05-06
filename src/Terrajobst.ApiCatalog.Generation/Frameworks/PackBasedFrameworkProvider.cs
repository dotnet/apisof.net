using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class PackBasedFrameworkProvider : FrameworkProvider
{
    private readonly string _frameworksPath;

    public PackBasedFrameworkProvider(string frameworksPath)
    {
        _frameworksPath = frameworksPath;
    }

    public override IEnumerable<(string FrameworkName, FrameworkAssembly[] Assemblies)> Resolve()
    {
        var packIndexPath = Path.Combine(_frameworksPath, FrameworkManifest.FileName);
        if (!File.Exists(packIndexPath))
            yield break;

        var manifest = FrameworkManifest.Load(packIndexPath);
        var assemblies = new Dictionary<string, FrameworkAssembly>(StringComparer.OrdinalIgnoreCase);

        var entryByFx = manifest.Frameworks.ToDictionary(fx => NuGetFramework.Parse(fx.FrameworkName));

        foreach (var entry in manifest.Frameworks)
        {
            assemblies.Clear();

            AddAssemblies(entry.Packages, assemblies);

            // TODO: I think this is superfluous now:
            //
            // For frameworks with a platform, such as `net5.0-windows`, we also want to add in all references
            // from the base framework, such as `net5.0`.

            var fx = NuGetFramework.Parse(entry.FrameworkName);
            var baseFx = fx.GetBaseFramework();
            if (baseFx is not null)
            {
                var baseEntry = entryByFx[baseFx];
                AddAssemblies(baseEntry.Packages, assemblies);
            }

            var assemblyArray = assemblies.Values.ToArray();
            yield return (entry.FrameworkName, assemblyArray);
        }

        static void AddAssemblies(IReadOnlyList<FrameworkManifestPackage> packages, Dictionary<string, FrameworkAssembly> assemblies)
        {
            foreach (var package in packages)
            foreach (var assembly in package.Assemblies)
            {
                if (assemblies.ContainsKey(assembly.Path))
                    continue;

                var frameworkAssembly = new FrameworkAssembly(assembly.Path, package.Id, assembly.Profiles);
                assemblies.Add(frameworkAssembly.Path, frameworkAssembly);
            }
        }
    }
}