global using StringOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.String>;
global using BlobOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.Blob>;
global using FrameworkOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.Framework>;
global using PackageOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.Package>;
global using AssemblyOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.Assembly>;
global using UsageSourceOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.UsageSource>;
global using ApiOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.Api>;
global using ApiDeclarationOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.ApiDeclaration>;
global using ApiUsageOffset = Terrajobst.ApiCatalog.Offset<Terrajobst.ApiCatalog.OffsetTag.ApiUsage>;

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

    public override bool Equals(object obj)
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
