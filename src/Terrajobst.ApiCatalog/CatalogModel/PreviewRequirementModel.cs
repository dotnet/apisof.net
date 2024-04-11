namespace Terrajobst.ApiCatalog;

public readonly struct PreviewRequirementModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PreviewRequirementModel(ApiCatalogModel catalog, int offset)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(offset, catalog.PreviewRequirementTable, ApiCatalogSchema.PreviewRequirementRow.Size);

        _catalog = catalog;
        _offset = offset;
    }

    public string Message => ApiCatalogSchema.PreviewRequirementRow.Message.Read(_catalog, _offset);

    public string Url => ApiCatalogSchema.PreviewRequirementRow.Url.Read(_catalog, _offset);

    public override bool Equals(object? obj)
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