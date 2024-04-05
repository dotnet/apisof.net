namespace Terrajobst.ApiCatalog;

public sealed class MarkupToken(MarkupTokenKind kind, string? text = null, Guid? reference = null) : IEquatable<MarkupToken>
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

    public bool Equals(MarkupToken? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Kind == other.Kind &&
               Text == other.Text &&
               Reference == other.Reference;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is MarkupToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Kind, Text, Reference);
    }

    public static bool operator ==(MarkupToken? left, MarkupToken? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MarkupToken? left, MarkupToken? right)
    {
        return !Equals(left, right);
    }
}