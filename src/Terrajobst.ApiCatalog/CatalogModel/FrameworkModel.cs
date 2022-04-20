using System.Collections;

namespace Terrajobst.ApiCatalog;

public readonly struct FrameworkModel : IEquatable<FrameworkModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal FrameworkModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public string Name
    {
        get
        {
            var stringOffset = _catalog.FrameworkTable.ReadInt32(_offset);
            return _catalog.GetString(stringOffset);
        }
    }

    public AssemblyEnumerator Assemblies
    {
        get
        {
            return new AssemblyEnumerator(_catalog, _offset + 4);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is FrameworkModel model && Equals(model);
    }

    public bool Equals(FrameworkModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(FrameworkModel left, FrameworkModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FrameworkModel left, FrameworkModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }

    public struct AssemblyEnumerator : IEnumerable<AssemblyModel>, IEnumerator<AssemblyModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public AssemblyEnumerator(ApiCatalogModel catalog, int offset)
        {
            _catalog = catalog;
            _offset = offset;
            _count = catalog.FrameworkTable.ReadInt32(offset);
            _index = -1;
        }

        IEnumerator<AssemblyModel> IEnumerable<AssemblyModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AssemblyEnumerator GetEnumerator()
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

        public AssemblyModel Current
        {
            get
            {
                var offset = _catalog.FrameworkTable.ReadInt32(_offset + 4 + 4 * _index);
                return new AssemblyModel(_catalog, offset);
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