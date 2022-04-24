using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class PlatformContext
{
    public static PlatformContext Create(ApiCatalogModel catalog, string framework)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(framework);

        if (!catalog.Frameworks.Any(fx => string.Equals(fx.Name, framework, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"invalid framework: '{framework}'", nameof(framework));

        var nuGetFramework = NuGetFramework.Parse(framework);
        return new PlatformContext(catalog, nuGetFramework);
    }

    private readonly ApiCatalogModel _catalog;
    private readonly NuGetFramework _framework;
    private readonly IReadOnlyList<string> _platforms;
    private readonly IReadOnlyList<(string Platform, string ImpliedPlatform)> _impliedPlatforms;

    private PlatformContext(ApiCatalogModel catalog, NuGetFramework framework)
    {
        _catalog = catalog;
        _framework = framework;
        _platforms = GetKnownPlatforms().OrderBy(p => p).ToArray();
        _impliedPlatforms = GetImpliedPlatforms().OrderBy(t => t.Platform).ToArray();
    }

    public ApiCatalogModel Catalog => _catalog;

    public NuGetFramework Framework => _framework;

    public IReadOnlyList<string> Platforms => _platforms;

    public IReadOnlyList<(string Platform, string ImpliedPlatform)> ImpliedPlatforms => _impliedPlatforms;

    private ApiModel? GetOperatingSystemType()
    {
        var systemNamespace = _catalog.RootApis.SingleOrDefault(a => a.Kind == ApiKind.Namespace && a.Name == "System");
        if (systemNamespace == default)
            return null;

        var operatingSystemType = systemNamespace.Children.SingleOrDefault(a => a.Kind == ApiKind.Class && a.Name == "OperatingSystem");
        if (operatingSystemType == default)
            return null;

        return operatingSystemType;
    }

    private IEnumerable<string> GetKnownPlatforms()
    {
        // The set of known platforms is determined by looking at the OperatingSystem.IsXxx() methods.
        //
        // NOTE: We don't filter to the APIs in the selected framework. We do this to support platforms like
        //       .NET Standard 2.0 which can ship annotations with locally defined attributes. By looking at
        //       all catalog APIs we always use the latest set of known platforms.

        var operatingSystemType = GetOperatingSystemType();
        if (operatingSystemType != null)
        {
            foreach (var member in operatingSystemType.Value.Children)
            {
                if (TryGetPlatformFromIsPlatformMethod(member, out var platformName))
                    yield return platformName;
            }
        }
    }

    private IEnumerable<(string Platform, string ImpliedPlatform)> GetImpliedPlatforms()
    {
        // We're computing the set of implied platforms by looking at OperatingSystem.IsXxx() methods
        // which have [SupportedOSPlatformGuard("yyy")] applied to them. This means for platform xxx the
        // platform yyy is implied.

        var frameworkName = _framework.GetShortFolderName();
        var frameworkAssemblies = _catalog.Frameworks
                                          .Single(fx => string.Equals(fx.Name, frameworkName, StringComparison.OrdinalIgnoreCase))
                                          .Assemblies.ToHashSet();

        var operatingSystemType = GetOperatingSystemType();
        if (operatingSystemType != null)
        {
            foreach (var member in operatingSystemType.Value.Children)
            {
                if (TryGetPlatformFromIsPlatformMethod(member, out var platformName))
                {
                    var declaration = member.Declarations.FirstOrDefault(d => frameworkAssemblies.Contains(d.Assembly));
                    if (declaration != default)
                    {
                        var markup = declaration.GetMyMarkup();

                        for (var i = 0; i < markup.Parts.Length - 6; i++)
                        {
                            var p1 = markup.Parts[i];
                            var p2 = markup.Parts[i + 1];
                            var p3 = markup.Parts[i + 2];
                            var p4 = markup.Parts[i + 3];
                            var p5 = markup.Parts[i + 4];
                            var p6 = markup.Parts[i + 5];

                            if (p1.Kind == MarkupPartKind.Punctuation && p1.Text == "[" &&
                                p2.Kind == MarkupPartKind.Reference && p2.Text == "SupportedOSPlatformGuard" &&
                                p3.Kind == MarkupPartKind.Punctuation && p3.Text == "(" &&
                                p4.Kind == MarkupPartKind.LiteralString &&
                                p5.Kind == MarkupPartKind.Punctuation && p5.Text == ")" &&
                                p6.Kind == MarkupPartKind.Punctuation && p6.Text == "]")
                            {
                                var literalWithoutQuotes = p4.Text.Substring(1, p4.Text.Length - 2);
                                var impliedPlatformName = literalWithoutQuotes;
                                yield return (platformName, impliedPlatformName);
                            }
                        }
                    }
                }
            }
        }
    }

    private static bool TryGetPlatformFromIsPlatformMethod(ApiModel operatingSystemMember, out string platformName)
    {
        const string prefix = "Is";
        const string suffix = "()";
        if (operatingSystemMember.Kind == ApiKind.Method &&
            operatingSystemMember.Name.StartsWith(prefix) &&
            operatingSystemMember.Name.EndsWith(suffix))
        {
            var start = prefix.Length;
            var length = operatingSystemMember.Name.Length - prefix.Length - suffix.Length;
            platformName = operatingSystemMember.Name.Substring(start, length).ToLower();
            return true;
        }

        platformName = null;
        return false;
    }

    public PlatformAnnotation GetPlatformAnnotation(ApiModel api)
    {
        var availability = ApiAvailability.Create(api);
        var frameworkAvailability = availability.Frameworks.FirstOrDefault(fx => fx.Framework == _framework);

        if (frameworkAvailability is null)
            throw new ArgumentException($"The API '{api.GetFullName()}' doesn't have a declaration for '{_framework}'.", nameof(api));

        var result = CreatePlatformAnnotation(api, frameworkAvailability);
        if (result is not null)
            return result.Value;

        return _framework.Framework == ".NETCoreApp" && _framework.Version.Major >= 5
            ? PlatformAnnotation.Unrestricted
            : PlatformAnnotation.None;
    }

    private PlatformAnnotation? CreatePlatformAnnotation(ApiModel api, ApiFrameworkAvailability frameworkAvailability)
    {
        var assembly = frameworkAvailability.Declaration.Assembly;

        foreach (var a in api.AncestorsAndSelf().Where(a => a.Kind != ApiKind.Namespace))
        {
            var d = a.Declarations.First(d => d.Assembly == assembly);
            var result = CreatePlatformAnnotation(d);
            if (result is not null)
                return result;
        }

        if (assembly.PlatformSupport.Any())
            return CreatePlatformAnnotation(assembly.PlatformSupport);

        return null;
    }

    private PlatformAnnotation? CreatePlatformAnnotation(ApiDeclarationModel declaration)
    {
        if (declaration.PlatformSupport.Any())
            return CreatePlatformAnnotation(declaration.PlatformSupport);

        return null;
    }

    private PlatformAnnotation CreatePlatformAnnotation(IEnumerable<PlatformSupportModel> platformSupport)
    {
        var parsedEntries = platformSupport.Select(ps => (NameAndVersion: ParsePlatform(ps.PlatformName), ps.IsSupported))
                                           .GroupBy(t => t.NameAndVersion.Name, t => (t.NameAndVersion.Version, t.IsSupported))
                                           .Select(g => (Platform: g.Key, Range: g.ToList()))
                                           .ToList();

        var impliedEntries = new List<(string Platform, List<(Version Version, bool IsSupported)> Range)>();

        foreach (var parsedEntry in parsedEntries)
        {
            foreach (var (platform, impliedPlatform) in _impliedPlatforms)
            {
                if (string.Equals(parsedEntry.Platform, platform, StringComparison.OrdinalIgnoreCase))
                    impliedEntries.Add((impliedPlatform, parsedEntry.Range.ToList()));
            }
        }

        var allEntries = parsedEntries.Concat(impliedEntries)
                                      .GroupBy(t => t.Platform, t => t.Range)
                                      .Select(g => new PlatformAnnotationEntry(g.Key, new PlatformSupportRange(g.SelectMany(t => t))))
                                      .Where(t => !t.Range.IsEmpty)
                                      .OrderBy(t => t.Name)
                                      .ToArray();

        return new PlatformAnnotation(allEntries);
    }

    private static (string Name, Version Version) ParsePlatform(string nameAndVersion)
    {
        var framework = NuGetFramework.Parse("net5.0-" + nameAndVersion);
        return (framework.Platform, framework.PlatformVersion);
    }
}