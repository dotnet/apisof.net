using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

internal sealed class SyntaxWriter
{
    private readonly MarkupWriter _writer;
    private bool _startOfLine = true;

    public SyntaxWriter(MarkupWriter writer)
    {
        _writer = writer;
    }

    public void Write(MarkupTokenKind kind)
    {
        WriteCore(kind, null, null);
    }

    public void WriteReference(ISymbol symbol, string text)
    {
        WriteCore(MarkupTokenKind.ReferenceToken, text, symbol);
    }

    public void WriteLiteralString(string text)
    {
        WriteCore(MarkupTokenKind.LiteralString, text, null);
    }

    public void WriteLiteralNumber(string text)
    {
        WriteCore(MarkupTokenKind.LiteralNumber, text, null);
    }

    public void WriteSpace()
    {
        Write(MarkupTokenKind.Space);
    }

    public void WriteLine()
    {
        Write(MarkupTokenKind.LineBreak);
    }

    private void WriteCore(MarkupTokenKind kind, string? text, ISymbol? symbol)
    {
        if (_startOfLine)
        {
            WriteIndent();
            _startOfLine = false;
        }

        _writer.Write(kind, text, symbol);

        if (kind == MarkupTokenKind.LineBreak)
            _startOfLine = true;
    }

    private void WriteIndent()
    {
        for (var i = 0; i < 4 * Indent; i++)
            _writer.Write(MarkupTokenKind.Space);
    }

    public int Indent { get; set; }
}