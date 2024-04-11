namespace Terrajobst.ApiCatalog;

public readonly struct ExtensionMethodModel : IEquatable<ExtensionMethodModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ExtensionMethodModel(ApiCatalogModel catalog, int offset)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(offset, catalog.ExtensionMethodTable, ApiCatalogSchema.ExtensionMethodRow.Size);

        _catalog = catalog;
        _offset = offset;
    }

    public int Id => _offset;

    public ApiCatalogModel Catalog => _catalog;

    public Guid Guid => ApiCatalogSchema.ExtensionMethodRow.Guid.Read(_catalog, _offset);

    public ApiModel ExtendedType => ApiCatalogSchema.ExtensionMethodRow.ExtendedType.Read(_catalog, _offset);

    public ApiModel ExtensionMethod => ApiCatalogSchema.ExtensionMethodRow.ExtensionMethod.Read(_catalog, _offset);

    public override bool Equals(object? obj)
    {
        return obj is ExtensionMethodModel model && Equals(model);
    }

    public bool Equals(ExtensionMethodModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(ExtensionMethodModel left, ExtensionMethodModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ExtensionMethodModel left, ExtensionMethodModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return ExtensionMethod.ToString();
    }
}