namespace Terrajobst.ApiCatalog;

internal static class OffsetTag
{
    public sealed class String;
    public sealed class Blob;
    public sealed class Framework;
    public sealed class Package;
    public sealed class Assembly;
    public sealed class UsageSource;
    public sealed class Api;
    public sealed class ApiDeclaration;
    public sealed class ApiUsage;
}

internal readonly struct Offset<T>(int value) : IEquatable<Offset<T>>
{
    public static Offset<T> Nil { get; } = new(-1);

    public bool IsNil => this == Nil;

    public int Value { get; } = value;

    public bool Equals(Offset<T> other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Offset<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(Offset<T> left, Offset<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Offset<T> left, Offset<T> right)
    {
        return !left.Equals(right);
    }
}
