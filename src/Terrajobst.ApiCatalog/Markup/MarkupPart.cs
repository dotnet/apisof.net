using System;

namespace Terrajobst.ApiCatalog;

public sealed class MarkupPart
{
    public MarkupPart(MarkupPartKind kind, string text, Guid? reference = null)
    {
        Kind = kind;
        Text = text;
        Reference = reference;
    }

    public MarkupPartKind Kind { get; }
    public string Text { get; }
    public Guid? Reference { get; }

    public override string ToString()
    {
        if (Reference == null)
            return $"{Kind}: {Text}";
        else
            return $"{Kind}: {Text} --> {Reference}";
    }
}