namespace Terrajobst.ApiCatalog;

public readonly struct ExperimentalModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ExperimentalModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public string DiagnosticId
    {
        get
        {
            var stringOffset = _catalog.ExperimentalTable.ReadInt32(_offset + 8);
            return _catalog.GetString(stringOffset);
        }
    }

    public string UrlFormat
    {
        get
        {
            var stringOffset = _catalog.ExperimentalTable.ReadInt32(_offset + 12);
            return _catalog.GetString(stringOffset);
        }
    }

    public string Url
    {
        get
        {
            return UrlFormat.Length > 0 && DiagnosticId.Length > 0
                        ? string.Format(UrlFormat, DiagnosticId)
                        : UrlFormat;
        }
    }

    public override bool Equals(object obj)
    {
        return obj is ExperimentalModel model && Equals(model);
    }

    public bool Equals(ExperimentalModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(ExperimentalModel left, ExperimentalModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ExperimentalModel left, ExperimentalModel right)
    {
        return !(left == right);
    }
}