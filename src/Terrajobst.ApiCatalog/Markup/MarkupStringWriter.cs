#nullable enable
using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

internal sealed class MarkupStringWriter : MarkupWriter
{
    private readonly StringWriter _writer = new();

    public override void Write(MarkupTokenKind kind, string? text = null, ISymbol? symbol = null)
    {
        var tokenText = text ?? kind.GetTokenText(); 
        _writer.Write(tokenText);
    }

    public override string ToString()
    {
        return _writer.ToString();
    }
}