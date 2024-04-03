namespace Terrajobst.ApiCatalog;

public readonly struct ExperimentalModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ExperimentalModel(ApiCatalogModel catalog, int offset)
    {
        ApiCatalogSchema.EnsureValidOffset(catalog.ExperimentalTable, ApiCatalogSchema.ExperimentalRow.Size, offset);

        _catalog = catalog;
        _offset = offset;
    }

    public string DiagnosticId => ApiCatalogSchema.ExperimentalRow.DiagnosticId.Read(_catalog, _offset);

    public string UrlFormat => ApiCatalogSchema.ExperimentalRow.UrlFormat.Read(_catalog, _offset);

    public string Url
    {
        get
        {
            return UrlFormat.Length > 0 && DiagnosticId.Length > 0
                        ? string.Format(UrlFormat, DiagnosticId)
                        : UrlFormat;
        }
    }

    public override bool Equals(object? obj)
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