namespace Terrajobst.ApiCatalog;

public readonly struct ObsoletionModel : IEquatable<ObsoletionModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ObsoletionModel(ApiCatalogModel catalog, int offset)
    {
        ApiCatalogSchema.EnsureValidOffset(catalog.ObsoletionTable, ApiCatalogSchema.ObsoletionRow.Size, offset);

        _catalog = catalog;
        _offset = offset;
    }

    public string Message => ApiCatalogSchema.ObsoletionRow.Message.Read(_catalog, _offset);

    public bool IsError => ApiCatalogSchema.ObsoletionRow.IsError.Read(_catalog, _offset);

    public string DiagnosticId => ApiCatalogSchema.ObsoletionRow.DiagnosticId.Read(_catalog, _offset);

    public string UrlFormat => ApiCatalogSchema.ObsoletionRow.UrlFormat.Read(_catalog, _offset);

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