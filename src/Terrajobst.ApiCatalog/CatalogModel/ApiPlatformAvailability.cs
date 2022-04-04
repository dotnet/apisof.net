using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiPlatformAvailability
{
    private readonly (string Name, Version Version)[] _platforms;
    private readonly Dictionary<string, (Version Version, bool IsSupported)[]> _supportRangeByPlatform;
    private readonly bool _isInclusion;

    private ApiPlatformAvailability((string Name, Version Version)[] platforms,
                                    Dictionary<string, (Version Version, bool IsSupported)[]> supportRangeByPlatform)
    {
        _platforms = platforms;
        _supportRangeByPlatform = supportRangeByPlatform;
        _isInclusion = supportRangeByPlatform.Any(r => r.Value[0].IsSupported);
    }

    public IEnumerable<(string Name, Version Version)> GetSupport()
    {
        var platformNames = new SortedSet<string>(_platforms.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);

        foreach (var name in platformNames)
        {
            if (_supportRangeByPlatform.TryGetValue(name, out var range))
            {
                foreach (var (v, isSupported) in range)
                {
                    if (isSupported)
                        yield return (name, v);
                }
            }
            else if (!_isInclusion)
            {
                yield return (name, new Version(0, 0, 0, 0));
            }
        }
    }

    public bool IsSupported(string nameAndVersion)
    {
        var (name, version) = ParsePlatform(nameAndVersion);
        return IsSupported(name, version);
    }

    public bool IsSupported(string name, Version version)
    {
        if (_supportRangeByPlatform.TryGetValue(name, out var range))
        {
            var previousSupported = !_isInclusion;

            foreach (var (v, isSupported) in range)
            {
                if (version < v)
                    return previousSupported;

                previousSupported = isSupported;
            }

            return previousSupported;
        }

        return !_isInclusion;
    }

    public static ApiPlatformAvailability Create(ApiDeclarationModel declaration)
    {
        var assembly = declaration.Assembly;
        var platforms = declaration.Catalog.Platforms.Select(p => ParsePlatform(p.Name)).ToArray();

        foreach (var api in declaration.Api.AncestorsAndSelf().Where(a => a.Kind != ApiKind.Namespace))
        {
            var d = api.Declarations.Single(d => d.Assembly == assembly);
            if (d.PlatformSupport.Any())
            {
                var support = d.PlatformSupport
                               .Select(ps => (Platform: ParsePlatform(ps.PlatformName), ps.IsSupported))
                               .GroupBy(t => t.Platform.Name)
                               .Select(g => (Platform: g.Key, Range: g.Select(t => (t.Platform.Version, t.IsSupported)).OrderBy(t => t.Version).ToArray()))
                               .ToDictionary(t => t.Platform, t => t.Range);

                return new ApiPlatformAvailability(platforms, support);
            }
        }

        return new ApiPlatformAvailability(platforms, new());
    }

    private static (string Name, Version Version) ParsePlatform(string nameAndVersion)
    {
        var framework = NuGetFramework.Parse("net5.0-" + nameAndVersion);
        return (framework.Platform, framework.PlatformVersion);
    }
}