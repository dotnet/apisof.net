using System.Collections;

namespace Terrajobst.ApiCatalog;

public readonly struct AssemblyModel : IEquatable<AssemblyModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal AssemblyModel(ApiCatalogModel catalog, int offset)
    {
        ApiCatalogSchema.EnsureValidOffset(catalog.AssemblyTable, ApiCatalogSchema.AssemblyRow.Size, offset);

        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public int Id => _offset;

    public Guid Guid => ApiCatalogSchema.AssemblyRow.Guid.Read(_catalog, _offset);

    public string Name => ApiCatalogSchema.AssemblyRow.Name.Read(_catalog, _offset);

    public string PublicKeyToken => ApiCatalogSchema.AssemblyRow.PublicKeyToken.Read(_catalog, _offset);

    public string Version => ApiCatalogSchema.AssemblyRow.Version.Read(_catalog, _offset);

    public RootApiEnumerator RootApis
    {
        get
        {
            var enumerator = ApiCatalogSchema.AssemblyRow.RootApis.Read(_catalog, _offset);
            return new RootApiEnumerator(enumerator);
        }
    }

    public FrameworkEnumerator Frameworks
    {
        get
        {
            var enumerator = ApiCatalogSchema.AssemblyRow.Frameworks.Read(_catalog, _offset);
            return new FrameworkEnumerator(enumerator);
        }
    }

    public PackageEnumerator Packages
    {
        get
        {
            var enumerator = ApiCatalogSchema.AssemblyRow.Packages.Read(_catalog, _offset);
            return new PackageEnumerator(enumerator);
        }
    }

    public IEnumerable<PlatformSupportModel> PlatformSupport
    {
        get
        {
            return _catalog.GetPlatformSupport(null, this);
        }
    }

    public PreviewRequirementModel? PreviewRequirement
    {
        get
        {
            return _catalog.GetPreviewRequirement(null, this);
        }
    }

    public ExperimentalModel? Experimental
    {
        get
        {
            return _catalog.GetExperimental(null, this);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is AssemblyModel model && Equals(model);
    }

    public bool Equals(AssemblyModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(AssemblyModel left, AssemblyModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssemblyModel left, AssemblyModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Name}, Version={Version}, PublicKeyToken={PublicKeyToken}";
    }

    public struct RootApiEnumerator : IEnumerable<ApiModel>, IEnumerator<ApiModel>
    {
        private ApiCatalogSchema.ArrayEnumerator<ApiModel> _enumerator;

        internal RootApiEnumerator(ApiCatalogSchema.ArrayEnumerator<ApiModel> enumerator)
        {
            _enumerator = enumerator;
        }

        IEnumerator<ApiModel> IEnumerable<ApiModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public RootApiEnumerator GetEnumerator()
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

        public ApiModel Current
        {
            get { return _enumerator.Current; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct FrameworkEnumerator : IEnumerable<FrameworkModel>, IEnumerator<FrameworkModel>
    {
        private ApiCatalogSchema.ArrayEnumerator<FrameworkModel> _enumerator;

        internal FrameworkEnumerator(ApiCatalogSchema.ArrayEnumerator<FrameworkModel> enumerator)
        {
            _enumerator = enumerator;
        }

        IEnumerator<FrameworkModel> IEnumerable<FrameworkModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public FrameworkEnumerator GetEnumerator()
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

        public FrameworkModel Current
        {
            get
            {
                return _enumerator.Current;
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

    public struct PackageEnumerator : IEnumerable<(PackageModel Package, FrameworkModel Framework)>, IEnumerator<(PackageModel Package, FrameworkModel Framework)>
    {
        private ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.AssemblyPackageTupleLayout> _enumerator;

        internal PackageEnumerator(ApiCatalogSchema.ArrayOfStructuresEnumerator<ApiCatalogSchema.AssemblyPackageTupleLayout> enumerator)
        {
            _enumerator = enumerator;
        }

        IEnumerator<(PackageModel Package, FrameworkModel Framework)> IEnumerable<(PackageModel Package, FrameworkModel Framework)>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PackageEnumerator GetEnumerator()
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

        public (PackageModel Package, FrameworkModel Framework) Current
        {
            get
            {
                var offset = _enumerator.Current;
                var package = _enumerator.Layout.Package.Read(_enumerator.Catalog, offset);
                var framework = _enumerator.Layout.Framework.Read(_enumerator.Catalog, offset);
                return (package, framework);
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