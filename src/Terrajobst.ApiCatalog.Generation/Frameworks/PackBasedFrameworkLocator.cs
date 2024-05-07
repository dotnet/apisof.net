using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class PackBasedFrameworkLocator : FrameworkLocator
{
    private readonly string _frameworksPath;
    private Dictionary<NuGetFramework, string[]>? _mappings;

    public PackBasedFrameworkLocator(string frameworksPath)
    {
        _frameworksPath = frameworksPath;
    }

    public override string[]? Locate(NuGetFramework packageFramework)
    {
        if (_mappings is null)
        {
            _mappings = new Dictionary<NuGetFramework, string[]>();
            var provider = new PackBasedFrameworkProvider(_frameworksPath);
            foreach (var (tfm, assemlies) in provider.Resolve())
                _mappings.Add(NuGetFramework.Parse(tfm), assemlies.Select(a => a.Path).ToArray());
        }

        if (_mappings.TryGetValue(packageFramework, out var mappingPaths))
            return mappingPaths;

        if (packageFramework.HasPlatform)
        {
            if (packageFramework.PlatformVersion == FrameworkConstants.EmptyVersion)
            {
                // If the framework has no version number, we just return the lowest plaform version
                // the framework supports.

                var mapping = _mappings.Where(kv => SameFrameworkSamePlatform(kv.Key, packageFramework))
                                       .OrderBy(kv => kv.Key.PlatformVersion)
                                       .Cast<KeyValuePair<NuGetFramework, string[]>?>()
                                       .FirstOrDefault();

                if (mapping is not null)
                    return mapping.Value.Value;
            }
            else
            {
                // If the framework has a platform version, we don't necessarily find an exact match.
                //
                // The mappings we have is for what platform version we ship in-box. For example, we might
                // ship net6.0-windows7.0, but the package might target net6.0-windows10.0.12345. In that
                // case we just return the framework assemblies for net6.0-windows7.0.
                //
                // If we ship multiple platform version (e.g. net6.0-windows7.0, net6.0-windows8.0) we'll
                // return the highest one that is still lower or equal to the one the package targets
                // (e.g. net6.0-windows8.0).

                var mapping = _mappings.Where(kv => SameFrameworkSamePlatformLowerOrEqualPlatformVersion(kv.Key, packageFramework))
                                       .OrderByDescending(kv => kv.Key.PlatformVersion)
                                       .Cast<KeyValuePair<NuGetFramework, string[]>?>()
                                       .FirstOrDefault();

                if (mapping is not null)
                    return mapping.Value.Value;
            }
        }

        return null;
    }

    private static bool SameFrameworkSamePlatform(NuGetFramework left, NuGetFramework right)
    {
        return left.HasPlatform && right.HasPlatform &&
               string.Equals(left.Framework, right.Framework, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Platform, right.Platform, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameFrameworkSamePlatformLowerOrEqualPlatformVersion(NuGetFramework left, NuGetFramework right)
    {
        return SameFrameworkSamePlatform(left, right) &&
               left.PlatformVersion <= right.PlatformVersion;
    }
}