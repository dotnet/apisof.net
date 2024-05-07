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
        var assemblies = new List<FrameworkAssembly>();

        var entryByFx = manifest.Frameworks.ToDictionary(fx => NuGetFramework.Parse(fx.FrameworkName));

        foreach (var entry in manifest.Frameworks)
        {
            assemblies.Clear();

            AddAssemblies(entry.Packages, assemblies);

            yield return (entry.FrameworkName, assemblies.ToArray());
        }

        static void AddAssemblies(IReadOnlyList<FrameworkManifestPackage> packages, List<FrameworkAssembly> assemblies)
        {
            foreach (var package in packages)
            foreach (var assembly in package.Assemblies)
            {
                var frameworkAssembly = new FrameworkAssembly(assembly.Path, package.Id, assembly.Profiles);
                assemblies.Add(frameworkAssembly);
            }
        }
    }
}