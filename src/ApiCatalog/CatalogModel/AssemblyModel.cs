using System;
using System.Collections.Generic;

namespace ApiCatalog.CatalogModel
{
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

        public Guid Guid
        {
            get
            {
                var guidSpan = _catalog.AssemblyTable.Slice(_offset, 16);
                return new Guid(guidSpan);
            }
        }

        public string Name
        {
            get
            {
                var stringOffset = _catalog.GetAssemblyTableInt32(_offset + 16);
                return _catalog.GetString(stringOffset);
            }
        }

        public string PublicKeyToken
        {
            get
            {
                var stringOffset = _catalog.GetAssemblyTableInt32(_offset + 20);
                return _catalog.GetString(stringOffset);
            }
        }

        public string Version
        {
            get
            {
                var stringOffset = _catalog.GetAssemblyTableInt32(_offset + 24);
                return _catalog.GetString(stringOffset);
            }
        }

        public IEnumerable<ApiModel> RootApis
        {
            get
            {
                var count = _catalog.GetAssemblyTableInt32(_offset + 28);

                for (var i = 0; i < count; i++)
                {
                    var offset = _catalog.GetAssemblyTableInt32(_offset + 32 + i * 4);
                    yield return new ApiModel(_catalog, offset);
                }
            }
        }

        public IEnumerable<FrameworkModel> Frameworks
        {
            get
            {
                var rootApiCount = _catalog.GetAssemblyTableInt32(_offset + 28);
                var frameworkTableOffset = _offset + 32 + rootApiCount * 4;
                var count = _catalog.GetAssemblyTableInt32(frameworkTableOffset);

                for (var i = 0; i < count; i++)
                {
                    var offset = _catalog.GetAssemblyTableInt32(frameworkTableOffset + 4 + i * 4);
                    yield return new FrameworkModel(_catalog, offset);
                }
            }
        }

        public IEnumerable<(PackageModel, FrameworkModel)> Packages
        {
            get
            {
                var rootApiCountOffset = _offset + 28;
                var rootApiCount = _catalog.GetAssemblyTableInt32(rootApiCountOffset);
                var frameworkCountOffset = rootApiCountOffset + 4 + rootApiCount * 4;
                var frameworkCount = _catalog.GetAssemblyTableInt32(frameworkCountOffset);

                var packageCountOffset = frameworkCountOffset + 4 + frameworkCount * 4;
                var packageCount = _catalog.GetAssemblyTableInt32(packageCountOffset);

                for (var i = 0; i < packageCount; i++)
                {
                    var packageOffset = _catalog.GetAssemblyTableInt32(packageCountOffset + 4 + i * 8);
                    var frameworkOffset = _catalog.GetAssemblyTableInt32(packageCountOffset + 4 + i * 8 + 4);
                    var packageModel = new PackageModel(_catalog, packageOffset);
                    var frameworkModel = new FrameworkModel(_catalog, frameworkOffset);
                    yield return (packageModel, frameworkModel);
                }
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
    }
}
