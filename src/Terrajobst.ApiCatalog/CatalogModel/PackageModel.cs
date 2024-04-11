using System.Collections;

namespace Terrajobst.ApiCatalog;

public readonly struct PackageModel : IEquatable<PackageModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PackageModel(ApiCatalogModel catalog, int offset)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(offset, catalog.PackageTable, ApiCatalogSchema.PackageRow.Size);

        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public int Id => _offset;

    public string Name => ApiCatalogSchema.PackageRow.Name.Read(_catalog, _offset);

    public string Version => ApiCatalogSchema.PackageRow.Version.Read(_catalog, _offset);

    public AssemblyEnumerator Assemblies
    {
        get
        {
            var enumerator = ApiCatalogSchema.PackageRow.Assemblies.Read(_catalog, _offset);
            return new AssemblyEnumerator(enumerator);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is FrameworkModel model && Equals(model);
    }

    public bool Equals(PackageModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(PackageModel left, PackageModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PackageModel left, PackageModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }

    public struct AssemblyEnumerator : IEnumerable<(FrameworkModel Framework, AssemblyModel Assembly)>, IEnumerator<(FrameworkModel Framework, AssemblyModel Assembly)>
    {
        private ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.PackageAssemblyTupleLayout> _enumerator;

        internal AssemblyEnumerator(ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.PackageAssemblyTupleLayout> enumerator)
        {
            _enumerator = enumerator;
        }

        IEnumerator<(FrameworkModel Framework, AssemblyModel Assembly)> IEnumerable<(FrameworkModel Framework, AssemblyModel Assembly)>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AssemblyEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public (FrameworkModel Framework, AssemblyModel Assembly) Current
        {
            get
            {
                var offset = _enumerator.Current;
                var framework = _enumerator.Layout.Framework.Read(_enumerator.Catalog, offset);
                var assembly = _enumerator.Layout.Assembly.Read(_enumerator.Catalog, offset);
                return (framework, assembly);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }
}