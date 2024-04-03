using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class PreviewRequirementEntry
{
    private PreviewRequirementEntry(string message, string? url)
    {
        Message = message;
        Url = url;
    }

    public string Message { get; }

    public string? Url { get; }

    public static PreviewRequirementEntry? Create(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.MatchesName(nameof(System),
                                                     nameof(System.Runtime),
                                                     nameof(System.Runtime.Versioning),
                                                     nameof(RequiresPreviewFeaturesAttribute)))
            {
                var message = attribute.GetSingleArgumentAsString() ?? string.Empty;
                var url = attribute.GetNamedArgument(nameof(RequiresPreviewFeaturesAttribute.Url));

                return new PreviewRequirementEntry(message, url);
            }
        }

        return null;
    }
}