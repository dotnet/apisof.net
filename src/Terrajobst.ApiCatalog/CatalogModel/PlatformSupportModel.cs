namespace Terrajobst.ApiCatalog;

public readonly struct PlatformSupportModel : IEquatable<PlatformSupportModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PlatformSupportModel(ApiCatalogModel catalog, int offset)
    {
        ApiCatalogSchema.EnsureValidBlobOffset(catalog, offset);

        _catalog = catalog;
        _offset = offset;
    }

    public string PlatformName => ApiCatalogSchema.PlatformIsSupportedTuple.Platform.Read(_catalog, _offset);

    public bool IsSupported => ApiCatalogSchema.PlatformIsSupportedTuple.IsSupported.Read(_catalog, _offset);

    public override bool Equals(object? obj)
    {
        return obj is ApiUsageModel model && Equals(model);
    }

    public bool Equals(PlatformSupportModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(PlatformSupportModel left, PlatformSupportModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformSupportModel left, PlatformSupportModel right)
    {
        return !(left == right);
    }
}