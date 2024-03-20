using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class ExperimentalEntry
{
    private ExperimentalEntry(string diagnosticId, string urlFormat)
    {
        DiagnosticId = diagnosticId;
        UrlFormat = urlFormat;
    }

    public string DiagnosticId { get; }
    public string UrlFormat { get; }

    public static ExperimentalEntry Create(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.MatchesName(nameof(System),
                                                     nameof(System.Diagnostics),
                                                     nameof(System.Diagnostics.CodeAnalysis),
                                                     nameof(ExperimentalAttribute)))
            {
                var diagnosticId = attribute.GetSingleArgumentAsString() ?? string.Empty;
                var urlFormat = attribute.GetNamedArgument(nameof(ExperimentalAttribute.UrlFormat)) ?? string.Empty;

                return new ExperimentalEntry(diagnosticId, urlFormat);
            }
        }

        return null;
    }
}