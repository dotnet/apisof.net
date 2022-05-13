namespace Terrajobst.ApiCatalog;

public readonly struct PreviewRequirementModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PreviewRequirementModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public string Message
    {
        get
        {
            var stringOffset = _catalog.PreviewRequirementTable.ReadInt32(_offset + 8);
            return _catalog.GetString(stringOffset);
        }
    }

    public string Url
    {
        get
        {
            var stringOffset = _catalog.PreviewRequirementTable.ReadInt32(_offset + 12);
            return _catalog.GetString(stringOffset);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is PreviewRequirementModel model && Equals(model);
    }

    public bool Equals(PreviewRequirementModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(PreviewRequirementModel left, PreviewRequirementModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PreviewRequirementModel left, PreviewRequirementModel right)
    {
        return !(left == right);
    }
}