using System;
using System.Collections.Generic;

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
            var stringOffset = _catalog.GetFrameworkTableInt32(_offset);
            return _catalog.GetString(stringOffset);
        }
    }

    public IEnumerable<AssemblyModel> Assemblies
    {
        get
        {
            var count = _catalog.GetFrameworkTableInt32(_offset + 4);

            for (var i = 0; i < count; i++)
            {
                var offset = _catalog.GetFrameworkTableInt32(_offset + 8 + i * 4);
                yield return new AssemblyModel(_catalog, offset);
            }
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
}