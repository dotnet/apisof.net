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
                var diagnosticId = GetFirstArgumentAsString(attribute) ?? string.Empty;
                var urlFormat = attribute.GetNamedArgument("UrlFormat") ?? string.Empty;

                return new ExperimentalEntry(diagnosticId, urlFormat);
            }
        }

        static string GetFirstArgumentAsString(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 1)
                return attribute.ConstructorArguments[0].Value as string;

            return null;
        }

        return null;
    }
}