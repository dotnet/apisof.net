namespace Terrajobst.ApiCatalog;

public readonly struct UsageSourceModel : IEquatable<UsageSourceModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal UsageSourceModel(ApiCatalogModel catalog, int offset)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(offset, catalog.UsageSourceTable, ApiCatalogSchema.UsageSourceRow.Size);

        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public int Id => _offset;

    public string Name => ApiCatalogSchema.UsageSourceRow.Name.Read(_catalog, _offset);

    public DateOnly Date => ApiCatalogSchema.UsageSourceRow.Date.Read(_catalog, _offset);

    public override bool Equals(object? obj)
    {
        return obj is AssemblyModel model && Equals(model);
    }

    public bool Equals(UsageSourceModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(UsageSourceModel left, UsageSourceModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UsageSourceModel left, UsageSourceModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Name} ({Date})";
    }
}