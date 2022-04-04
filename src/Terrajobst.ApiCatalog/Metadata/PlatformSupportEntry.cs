using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class PlatformSupportEntry
{
    public IReadOnlyList<string> SupportedPlatforms { get; }
    public IReadOnlyList<string> UnsupportedPlatforms { get; }

    private PlatformSupportEntry(IReadOnlyList<string> supportedPlatforms, IReadOnlyList<string> unsupportedPlatforms)
    {
        SupportedPlatforms = supportedPlatforms;
        UnsupportedPlatforms = unsupportedPlatforms;
    }

    public static PlatformSupportEntry Create(ISymbol symbol)
    {
        var supportedPlatforms = (List<string>)null;
        var unsupportedPlatforms = (List<string>)null;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.MatchesName(nameof(System),
                                                     nameof(System.Runtime),
                                                     nameof(System.Runtime.Versioning),
                                                     nameof(SupportedOSPlatformAttribute)))
            {
                if (attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments[0].Value is string argument)
                {
                    supportedPlatforms ??= new List<string>();
                    supportedPlatforms.Add(argument);
                }
            }

            if (attribute.AttributeClass.MatchesName(nameof(System),
                                                     nameof(System.Runtime),
                                                     nameof(System.Runtime.Versioning),
                                                     nameof(UnsupportedOSPlatformAttribute)))
            {
                if (attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments[0].Value is string argument)
                {
                    unsupportedPlatforms ??= new List<string>();
                    unsupportedPlatforms.Add(argument);
                }
            }
        }

        if (supportedPlatforms is null && unsupportedPlatforms is null)
            return null;

        return new PlatformSupportEntry(supportedPlatforms?.ToArray() ?? Array.Empty<string>(),
                                        unsupportedPlatforms?.ToArray() ?? Array.Empty<string>());
    }
}