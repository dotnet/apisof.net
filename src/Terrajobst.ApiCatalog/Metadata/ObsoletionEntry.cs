using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class ObsoletionEntry
{
    private ObsoletionEntry(string message, bool isError, string diagnosticId, string urlFormat)
    {
        Message = message;
        IsError = isError;
        DiagnosticId = diagnosticId;
        UrlFormat = urlFormat;
    }

    public string Message { get; }
    public bool IsError { get; }
    public string DiagnosticId { get; }
    public string UrlFormat { get; }

    public static ObsoletionEntry Create(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.MatchesName(nameof(System), nameof(ObsoleteAttribute)))
            {
                var message = (string)null;
                var isError = false;

                if (attribute.ConstructorArguments.Length == 1)
                {
                    message = attribute.ConstructorArguments[0].Value as string;
                }
                else if (attribute.ConstructorArguments.Length == 2)
                {
                    message = attribute.ConstructorArguments[0].Value as string;
                    isError = attribute.ConstructorArguments[1].Value is true;
                }

                var diagnosticId = attribute.GetNamedArgument(nameof(ObsoleteAttribute.DiagnosticId));
                var urlFormat = attribute.GetNamedArgument(nameof(ObsoleteAttribute.UrlFormat));

                return new ObsoletionEntry(message, isError, diagnosticId, urlFormat);
            }
        }

        return null;
    }
}