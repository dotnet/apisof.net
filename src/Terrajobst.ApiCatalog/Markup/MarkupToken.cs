#nullable enable

namespace Terrajobst.ApiCatalog;

public sealed class MarkupToken(MarkupTokenKind kind, string? text = null, Guid? reference = null)
{
    public MarkupTokenKind Kind { get; } = kind;
    public string Text { get; } = text ?? kind.GetTokenText() ?? string.Empty;
    public Guid? Reference { get; } = reference;

    public override string ToString()
    {
        return Reference is null
            ? $"{Kind}: {Text}"
            : $"{Kind}: {Text} --> {Reference}";
    }
}