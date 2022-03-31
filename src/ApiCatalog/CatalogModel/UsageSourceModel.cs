using System;

namespace ApiCatalog.CatalogModel
{
    public readonly struct UsageSourceModel : IEquatable<UsageSourceModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;

        internal UsageSourceModel(ApiCatalogModel catalog, int offset)
        {
            _catalog = catalog;
            _offset = offset;
        }

        public ApiCatalogModel Catalog => _catalog;

        public string Name
        {
            get
            {
                var stringOffset = _catalog.GetUsageSourcesTableInt32(_offset);
                return _catalog.GetString(stringOffset);
            }
        }

        public DateOnly Date
        {
            get
            {
                var dayNumber = _catalog.GetUsageSourcesTableInt32(_offset + 4);
                return DateOnly.FromDayNumber(dayNumber);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyModel model && Equals(model);
        }

        public bool Equals(UsageSourceModel other)
        {
            return ReferenceEquals(_catalog, other._catalog) &&
                   _offset == other._offset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_catalog, _offset);
        }

        public static bool operator ==(UsageSourceModel left, UsageSourceModel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UsageSourceModel left, UsageSourceModel right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Name} ({Date})";
        }
    }
}
