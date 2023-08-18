using System.Collections;

namespace Terrajobst.ApiCatalog;

public readonly struct AssemblyModel : IEquatable<AssemblyModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal AssemblyModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public int Id => _offset;

    public Guid Guid
    {
        get
        {
            return _catalog.AssemblyTable.ReadGuid(0);
        }
    }

    public string Name
    {
        get
        {
            var stringOffset = _catalog.AssemblyTable.ReadInt32(_offset + 16);
            return _catalog.GetString(stringOffset);
        }
    }

    public string PublicKeyToken
    {
        get
        {
            var stringOffset = _catalog.AssemblyTable.ReadInt32(_offset + 20);
            return _catalog.GetString(stringOffset);
        }
    }

    public string Version
    {
        get
        {
            var stringOffset = _catalog.AssemblyTable.ReadInt32(_offset + 24);
            return _catalog.GetString(stringOffset);
        }
    }

    public RootApiEnumerator RootApis
    {
        get
        {
            var offset = _offset + 28;
            return new RootApiEnumerator(_catalog, offset);
        }
    }

    public FrameworkEnumerator Frameworks
    {
        get
        {
            var rootApiCount = _catalog.AssemblyTable.ReadInt32(_offset + 28);
            var frameworkTableOffset = _offset + 32 + rootApiCount * 4;
            return new FrameworkEnumerator(_catalog, frameworkTableOffset);
        }
    }

    public PackageEnumerator Packages
    {
        get
        {
            var rootApiCountOffset = _offset + 28;
            var rootApiCount = _catalog.AssemblyTable.ReadInt32(rootApiCountOffset);
            var frameworkCountOffset = rootApiCountOffset + 4 + rootApiCount * 4;
            var frameworkCount = _catalog.AssemblyTable.ReadInt32(frameworkCountOffset);

            var packageTableOffset = frameworkCountOffset + 4 + frameworkCount * 4;
            return new PackageEnumerator(_catalog, packageTableOffset);
        }
    }

    public IEnumerable<PlatformSupportModel> PlatformSupport
    {
        get
        {
            return _catalog.GetPlatformSupport(-1, _offset);
        }
    }

    public PreviewRequirementModel? PreviewRequirement
    {
        get
        {
            return _catalog.GetPreviewRequirement(-1, _offset);
        }
    }

    public ExperimentalModel? Experimental
    {
        get
        {
            return _catalog.GetExperimental(-1, _offset);
        }
    }

    public override bool Equals(object obj)
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
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public RootApiEnumerator(ApiCatalogModel catalog, int offset)
        {
            _catalog = catalog;
            _offset = offset;
            _count = catalog.AssemblyTable.ReadInt32(offset);
            _index = -1;
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
            if (_index >= _count - 1)
                return false;

            _index++;
            return true;
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
            get
            {
                var offset = _catalog.AssemblyTable.ReadInt32(_offset + 4 + 4 * _index);
                return new ApiModel(_catalog, offset);
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

    public struct FrameworkEnumerator : IEnumerable<FrameworkModel>, IEnumerator<FrameworkModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public FrameworkEnumerator(ApiCatalogModel catalog, int offset)
        {
            _catalog = catalog;
            _offset = offset;
            _count = catalog.AssemblyTable.ReadInt32(offset);
            _index = -1;
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
            if (_index >= _count - 1)
                return false;

            _index++;
            return true;
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
                var offset = _catalog.AssemblyTable.ReadInt32(_offset + 4 + 4 * _index);
                return new FrameworkModel(_catalog, offset);
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
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public PackageEnumerator(ApiCatalogModel catalog, int offset)
        {
            _catalog = catalog;
            _offset = offset;
            _count = catalog.AssemblyTable.ReadInt32(offset);
            _index = -1;
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
            if (_index >= _count - 1)
                return false;

            _index++;
            return true;
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
                var offset = _offset + 4 + _index * 8;
                var packageOffset = _catalog.AssemblyTable.ReadInt32(offset);
                var frameworkOffset = _catalog.AssemblyTable.ReadInt32(offset + 4);
                var packageModel = new PackageModel(_catalog, packageOffset);
                var frameworkModel = new FrameworkModel(_catalog, frameworkOffset);
                return (packageModel, frameworkModel);
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