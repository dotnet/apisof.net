namespace Terrajobst.ApiCatalog;

public readonly struct ApiUsageModel : IEquatable<ApiUsageModel>
{
    private readonly ApiModel _api;
    private readonly int _offset;

    internal ApiUsageModel(ApiModel api, int offset)
    {
        ApiCatalogSchema.EnsureValidBlobOffset(api.Catalog, offset);

        _api = api;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => Api.Catalog;

    public ApiModel Api => _api;

    public UsageSourceModel Source => ApiCatalogSchema.ApiUsageStructure.UsageSource.Read(_api.Catalog, _offset);

    public float Percentage => ApiCatalogSchema.ApiUsageStructure.Percentage.Read(_api.Catalog, _offset);

    public override bool Equals(object obj)
    {
        return obj is ApiUsageModel model && Equals(model);
    }

    public bool Equals(ApiUsageModel other)
    {
        return _api == other._api &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_api, _offset);
    }

    public static bool operator ==(ApiUsageModel left, ApiUsageModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ApiUsageModel left, ApiUsageModel right)
    {
        return !(left == right);
    }
}