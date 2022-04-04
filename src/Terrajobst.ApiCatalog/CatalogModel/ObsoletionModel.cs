namespace Terrajobst.ApiCatalog;

public readonly struct ObsoletionModel : IEquatable<ObsoletionModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ObsoletionModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public string Message
    {
        get
        {
            var stringOffset = _catalog.ObsoletionTable.ReadInt32(_offset + 8);
            return _catalog.GetString(stringOffset);
        }
    }

    public bool IsError
    {
        get
        {
            return _catalog.ObsoletionTable.ReadByte(_offset + 12) == 1;
        }
    }

    public string DiagnosticId
    {
        get
        {
            var stringOffset = _catalog.ObsoletionTable.ReadInt32(_offset + 13);
            return _catalog.GetString(stringOffset);
        }
    }

    public string UrlFormat
    {
        get
        {
            var stringOffset = _catalog.ObsoletionTable.ReadInt32(_offset + 17);
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
        return obj is ObsoletionModel model && Equals(model);
    }

    public bool Equals(ObsoletionModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(ObsoletionModel left, ObsoletionModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ObsoletionModel left, ObsoletionModel right)
    {
        return !(left == right);
    }
}