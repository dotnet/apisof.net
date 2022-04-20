namespace Terrajobst.ApiCatalog;

public readonly struct ApiUsageModel : IEquatable<ApiUsageModel>
{
    private readonly ApiModel _api;
    private readonly int _offset;

    internal ApiUsageModel(ApiModel api, int offset)
    {
        _api = api;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => Api.Catalog;

    public ApiModel Api
    {
        get
        {
            return _api;
        }
    }

    public UsageSourceModel Source
    {
        get
        {
            var usageSourceOffset = _api.Catalog.ApiTable.ReadInt32(_offset);
            return new UsageSourceModel(_api.Catalog, usageSourceOffset);
        }
    }

    public float Percentage
    {
        get
        {
            var offset = _offset + 4;
            return _api.Catalog.ApiTable.ReadSingle(offset);
        }
    }

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