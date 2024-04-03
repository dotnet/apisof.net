using System.Diagnostics.CodeAnalysis;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class PlatformAnnotationContext
{
    public static PlatformAnnotationContext Create(ApiAvailabilityContext availabilityContext, string framework)
    {
        ThrowIfNull(availabilityContext);
        ThrowIfNull(framework);

        var catalog = availabilityContext.Catalog;
        if (!catalog.Frameworks.Any(fx => string.Equals(fx.Name, framework, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"invalid framework: '{framework}'", nameof(framework));

        var nuGetFramework = NuGetFramework.Parse(framework);
        return new PlatformAnnotationContext(availabilityContext, nuGetFramework);
    }

    private readonly ApiAvailabilityContext _availabilityContext;
    private readonly NuGetFramework _framework;
    private readonly IReadOnlyList<string> _platforms;
    private readonly IReadOnlyList<(string Platform, string ImpliedPlatform)> _impliedPlatforms;

    private PlatformAnnotationContext(ApiAvailabilityContext availabilityContext, NuGetFramework framework)
    {
        _availabilityContext = availabilityContext;
        _framework = framework;
        _platforms = GetKnownPlatforms().OrderBy(p => p).ToArray();
        _impliedPlatforms = GetImpliedPlatforms().OrderBy(t => t.Platform).ToArray();
    }

    public ApiCatalogModel Catalog => _availabilityContext.Catalog;

    public ApiAvailabilityContext AvailabilityContext => _availabilityContext;

    public NuGetFramework Framework => _framework;

    public IReadOnlyList<string> Platforms => _platforms;

    public IReadOnlyList<(string Platform, string ImpliedPlatform)> ImpliedPlatforms => _impliedPlatforms;

    private ApiModel? GetOperatingSystemType()
    {
        var systemNamespace = Catalog.RootApis.SingleOrDefault(a => a.Kind == ApiKind.Namespace && a.Name == "System");
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
        if (operatingSystemType is not null)
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
        var frameworkAssemblies = Catalog.Frameworks
                                         .Single(fx => string.Equals(fx.Name, frameworkName, StringComparison.OrdinalIgnoreCase))
                                         .Assemblies.ToHashSet();
        
        var supportedOsPlatformGuard = Catalog.AllApis.Where(a => a.Name == "SupportedOSPlatformGuardAttribute" &&
                                                                  a.Parent?.GetFullName() == "System.Runtime.Versioning")
                                                      .Select(a => (Guid?)a.Guid)
                                                      .SingleOrDefault();
        

        var operatingSystemType = GetOperatingSystemType();
        if (operatingSystemType is not null)
        {
            foreach (var member in operatingSystemType.Value.Children)
            {
                if (TryGetPlatformFromIsPlatformMethod(member, out var platformName))
                {
                    var declaration = member.Declarations.FirstOrDefault(d => frameworkAssemblies.Contains(d.Assembly));
                    if (declaration != default)
                    {
                        var markup = declaration.GetMyMarkup();

                        for (var i = 0; i < markup.Tokens.Length - 6; i++)
                        {
                            var t1 = markup.Tokens[i];
                            var t2 = markup.Tokens[i + 1];
                            var t3 = markup.Tokens[i + 2];
                            var t4 = markup.Tokens[i + 3];
                            var t5 = markup.Tokens[i + 4];
                            var t6 = markup.Tokens[i + 5];

                            if (t1.Kind == MarkupTokenKind.OpenBracketToken &&
                                t2.Kind == MarkupTokenKind.ReferenceToken && t2.Reference == supportedOsPlatformGuard &&
                                t3.Kind == MarkupTokenKind.OpenParenToken &&
                                t4.Kind == MarkupTokenKind.LiteralString &&
                                t5.Kind == MarkupTokenKind.CloseParenToken &&
                                t6.Kind == MarkupTokenKind.CloseBracketToken)
                            {
                                var literalWithoutQuotes = t4.Text.Substring(1, t4.Text.Length - 2);
                                var impliedPlatformName = literalWithoutQuotes;
                                yield return (platformName, impliedPlatformName);
                            }
                        }
                    }
                }
            }
        }
    }

    private static bool TryGetPlatformFromIsPlatformMethod(ApiModel operatingSystemMember, [MaybeNullWhen(false)] out string platformName)
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

        platformName = default;
        return false;
    }

    public PlatformAnnotation GetPlatformAnnotation(ApiModel api)
    {
        var frameworkAvailability = _availabilityContext.GetAvailability(api, _framework);
        if (frameworkAvailability is null)
            throw new ArgumentException($"The API '{api.GetFullName()}' doesn't have a declaration for '{_framework}'.", nameof(api));

        var result = CreatePlatformAnnotation(api, frameworkAvailability);
        if (result is not null)
            return result.Value;

        // If the API isn't annotated, we check if the framework itself is considered
        // platform-specific.
        //
        // However, we only want to consider .NET 5+ TFMs because they are the only ones
        // that can have the optional platform suffix. To see if the API is considered
        // platform specific by framework, we check if the API only appears in .NET 5
        // TFMs with a platform suffix. If it also occurs in the neutral one we consider
        // it unrestricted, otherwise we consider it only supported in the platforms it
        // occurs in.
        //
        // Please note that there is minor glitch here: the TFMs as recorded in the
        // catalog don't specify which platform versions they map to; we have
        // specifically exlcuded this knowledge from NuGet. The only party that knows
        // that, for instance, `net5.0-windows` really means `net5.0-windows7.0` is the
        // SDK. We could, however, decide to hardcode this knowledge in the catalog
        // because these mappings are considered static. Another option is to see if we
        // can extract that information from the indexed ref packs.

        if (_framework.Framework == ".NETCoreApp" && _framework.Version.Major >= 5)
        {
            var fullAvailability = _availabilityContext.GetAvailability(api);
            var platformSpecific = true;
            var platforms = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var otherAvailability in fullAvailability.Frameworks)
            {
                var otherFramework = otherAvailability.Framework;

                if (otherFramework.Framework == ".NETCoreApp" && otherFramework.Version.Major >= 5)
                {
                    if (otherFramework.HasPlatform)
                        platforms.Add(otherFramework.Platform);
                    else
                        platformSpecific = false;
                }
            }

            if (platformSpecific)
            {
                var zero = new[] { (new Version(0, 0, 0, 0), true) };
                var allVersions = new PlatformSupportRange(zero);
                var entries = platforms.Select(p => new PlatformAnnotationEntry(p, allVersions)).ToArray();
                return new PlatformAnnotation(entries);
            }

            return PlatformAnnotation.Unrestricted;
        }
        else
        {
            return PlatformAnnotation.None;
        }
    }

    private PlatformAnnotation? CreatePlatformAnnotation(ApiModel api, ApiFrameworkAvailability frameworkAvailability)
    {
        var assembly = frameworkAvailability.Declaration.Assembly;

        foreach (var a in api.AncestorsAndSelf().Where(a => a.Kind != ApiKind.Namespace))
        {
            var declaration = a.Declarations.First(d => d.Assembly == assembly);
            if (declaration.PlatformSupport.Any())
                return CreatePlatformAnnotation(declaration.PlatformSupport);
        }

        if (assembly.PlatformSupport.Any())
            return CreatePlatformAnnotation(assembly.PlatformSupport);

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

    public static (string Name, Version Version) ParsePlatform(string nameAndVersion)
    {
        var framework = NuGetFramework.Parse("net5.0-" + nameAndVersion);
        return (framework.Platform, framework.PlatformVersion);
    }
}