using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiPlatformAvailability
{
    public static ApiPlatformAvailability Unknown { get; } = new(ApiPlatformAvailabilityKind.Unknown, new());
    public static ApiPlatformAvailability Any { get; } = new(ApiPlatformAvailabilityKind.Any, new());

    private readonly Dictionary<string, (Version Version, bool IsSupported)[]> _supportRangeByPlatform;

    private ApiPlatformAvailability(ApiPlatformAvailabilityKind kind,
                                    Dictionary<string, (Version Version, bool IsSupported)[]> supportRangeByPlatform)
    {
        Kind = kind;
        _supportRangeByPlatform = supportRangeByPlatform;
        Versions = CreateVersions(supportRangeByPlatform);
    }

    public ApiPlatformAvailabilityKind Kind { get; }

    public IReadOnlyList<(string Name, string Versions)> Versions { get; }

    public bool IsSupported(string nameAndVersion)
    {
        var (name, version) = ParsePlatform(nameAndVersion);
        return IsSupported(name, version);
    }

    public bool IsSupported(string name, Version version)
    {
        if (Kind is ApiPlatformAvailabilityKind.Any or ApiPlatformAvailabilityKind.Unknown)
            return true;

        if (_supportRangeByPlatform.TryGetValue(name, out var range))
        {
            var previousSupported = Kind == ApiPlatformAvailabilityKind.AnyExcept;

            foreach (var (v, isSupported) in range)
            {
                if (version < v)
                    return previousSupported;

                previousSupported = isSupported;
            }

            return previousSupported;
        }

        return Kind == ApiPlatformAvailabilityKind.AnyExcept;
    }

    private static ApiPlatformAvailability Create(ApiDeclarationModel declaration)
    {
        var platformSupport = GetPlatformModel(declaration);
        if (platformSupport is not null)
        {
            var support = platformSupport
                          .Select(ps => (Platform: ParsePlatform(ps.PlatformName), ps.IsSupported))
                          .GroupBy(t => t.Platform.Name)
                          .Select(g => (Platform: g.Key,
                                           Range: g.Select(t => (t.Platform.Version, t.IsSupported)).OrderBy(t => t.Version).ToArray()))
                          .ToDictionary(t => t.Platform, t => t.Range);

            var kind = support.Any(ps => ps.Value[0].IsSupported)
                       ? ApiPlatformAvailabilityKind.OnlyOn
                       : ApiPlatformAvailabilityKind.AnyExcept;

            return new ApiPlatformAvailability(kind, support);
        }

        return null;

        static IEnumerable<PlatformSupportModel> GetPlatformModel(ApiDeclarationModel declaration)
        {
            var assembly = declaration.Assembly;

            foreach (var api in declaration.Api.AncestorsAndSelf().Where(a => a.Kind != ApiKind.Namespace))
            {
                var d = api.Declarations.Single(d => d.Assembly == assembly);
                if (d.PlatformSupport.Any())
                {
                    return d.PlatformSupport;
                }
            }

            if (assembly.PlatformSupport.Any())
                return assembly.PlatformSupport;

            return null;
        }
    }

    public static ApiPlatformAvailability Create(ApiDeclarationModel declaration, NuGetFramework framework)
    {
        var result = Create(declaration);
        if (result is not null)
            return result;

        return framework.Framework == ".NETCoreApp" && framework.Version.Major >= 5
            ? Any
            : Unknown;
    }

    private static (string Name, Version Version) ParsePlatform(string nameAndVersion)
    {
        var framework = NuGetFramework.Parse("net5.0-" + nameAndVersion);
        return (framework.Platform, framework.PlatformVersion);
    }

    private static IReadOnlyList<(string Name, string Versions)> CreateVersions(Dictionary<string, (Version Version, bool IsSupported)[]> supportRangeByPlatform)
    {
        return supportRangeByPlatform.Select(kv => (FormatPlatform(kv.Key), FormatVersions(kv.Value))).ToArray();

        static string FormatPlatform(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "android" => "Android",
                "browser" => "Browser",
                "freebsd" => "FreeBSD",
                "illumos" => "illumos",
                "ios" => "iOS",
                "linux" => "Linux",
                "maccatalyst" => "Mac Catalyst",
                "macos" => "macOS",
                "solaris" => "Solaris",
                "tvos" => "tvOS",
                "watchos" => "watchOS",
                "windows" => "Windows",
                _ => name
            };
        }

        static string FormatVersions((Version Version, bool IsSupported)[] versions)
        {
            var supportedVersions = versions.Where(v => v.IsSupported).Select(t => t.Version);
            var result = string.Join(", ", supportedVersions.Select(FormatVersion));
            if (string.IsNullOrEmpty(result) || result == "0")
                return string.Empty;

            return result;
        }

        static string FormatVersion(System.Version version)
        {
            if (version.Revision == 0)
            {
                if (version.Build == 0)
                {
                    if (version.Minor == 0)
                        return $"{version.Major}";

                    return $"{version.Major}.{version.Minor}";
                }

                return $"{version.Major}.{version.Minor}.{version.Build}";
            }

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}