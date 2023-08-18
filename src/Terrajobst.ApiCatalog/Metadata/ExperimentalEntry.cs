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
                                                     "ExperimentalAttribute"))
            {
                var diagnosticId = attribute.ConstructorArguments[0].Value as string;
                var urlFormat = attribute.GetNamedArgument("UrlFormat");

                return new ExperimentalEntry(diagnosticId, urlFormat);
            }
        }

        return null;
    }
}