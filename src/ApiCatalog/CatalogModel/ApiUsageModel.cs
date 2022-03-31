using System;

namespace ApiCatalog.CatalogModel
{
    public readonly struct ApiUsageModel : IEquatable<ApiUsageModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;

        internal ApiUsageModel(ApiCatalogModel catalog, int offset)
        {
            _catalog = catalog;
            _offset = offset;
        }

        public ApiCatalogModel Catalog => _catalog;

        public UsageSourceModel Source
        {
            get
            {
                var usageSourceOffset = _catalog.GetApiTableInt32(_offset);
                return new UsageSourceModel(_catalog, usageSourceOffset);
            }
        }

        public float Percentage
        {
            get
            {
                var offset = _offset + 4;
                return _catalog.GetApiTableSingle(offset);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ApiUsageModel model && Equals(model);
        }

        public bool Equals(ApiUsageModel other)
        {
            return ReferenceEquals(_catalog, other._catalog) &&
                   _offset == other._offset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_catalog, _offset);
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
}
