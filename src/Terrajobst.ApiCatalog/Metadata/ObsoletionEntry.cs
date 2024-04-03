using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class ObsoletionEntry
{
    private ObsoletionEntry(string? message, bool isError, string? diagnosticId, string? urlFormat)
    {
        Message = message;
        IsError = isError;
        DiagnosticId = diagnosticId;
        UrlFormat = urlFormat;
    }

    public string? Message { get; }
    public bool IsError { get; }
    public string? DiagnosticId { get; }
    public string? UrlFormat { get; }

    public static ObsoletionEntry? Create(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.MatchesName(nameof(System), nameof(ObsoleteAttribute)))
            {
                var message = (string?)null;
                var isError = false;

                if (attribute.ConstructorArguments is [{ Value: string arg1_0 }])
                {
                    message = arg1_0;
                }
                else if (attribute.ConstructorArguments is [{ Value: string arg2_0 }, { Value: bool arg2_1 }])
                {
                    message = arg2_0;
                    isError = arg2_1;
                }

                var diagnosticId = attribute.GetNamedArgument(nameof(ObsoleteAttribute.DiagnosticId));
                var urlFormat = attribute.GetNamedArgument(nameof(ObsoleteAttribute.UrlFormat));

                return new ObsoletionEntry(message, isError, diagnosticId, urlFormat);
            }
        }

        return null;
    }
}