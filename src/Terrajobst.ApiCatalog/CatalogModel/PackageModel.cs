using System;
using System.Collections.Generic;

namespace Terrajobst.ApiCatalog;

public readonly struct PackageModel : IEquatable<PackageModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PackageModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => _catalog;

    public string Name
    {
        get
        {
            var stringOffset = _catalog.GetPackageTableInt32(_offset);
            return _catalog.GetString(stringOffset);
        }
    }

    public string Version
    {
        get
        {
            var stringOffset = _catalog.GetPackageTableInt32(_offset + 4);
            return _catalog.GetString(stringOffset);
        }
    }

    public IEnumerable<(FrameworkModel, AssemblyModel)> Assemblies
    {
        get
        {
            var assemblyCountOffset = _offset + 8;
            var assemblyCount = _catalog.GetPackageTableInt32(assemblyCountOffset);

            for (var i = 0; i < assemblyCount; i++)
            {
                var frameworkOffset = _catalog.GetPackageTableInt32(assemblyCountOffset + 4 + i * 8);
                var assemblyOffset = _catalog.GetPackageTableInt32(assemblyCountOffset + 4 + i * 8 + 4);
                var frameworkModel = new FrameworkModel(_catalog, frameworkOffset);
                var assemblyModel = new AssemblyModel(_catalog, assemblyOffset);
                yield return (frameworkModel, assemblyModel);
            }
        }
    }

    public override bool Equals(object obj)
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
}