#nullable enable
using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

internal abstract class MarkupWriter
{
    public abstract void Write(MarkupTokenKind kind, string? text = null, ISymbol? symbol = null);
}