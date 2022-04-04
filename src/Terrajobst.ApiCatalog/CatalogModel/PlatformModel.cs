namespace Terrajobst.ApiCatalog;

public readonly struct PlatformModel : IEquatable<PlatformModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PlatformModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public string Name
    {
        get
        {
            var stringOffset = _catalog.PlatformTable.ReadInt32(_offset);
            return _catalog.GetString(stringOffset);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is PlatformModel model && Equals(model);
    }

    public bool Equals(PlatformModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(PlatformModel left, PlatformModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformModel left, PlatformModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }
}