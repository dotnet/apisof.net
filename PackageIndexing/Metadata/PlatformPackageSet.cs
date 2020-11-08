using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuGet.Packaging;

namespace PackageIndexing
{
    public class PlatformPackageSet : FileSet
    {
        public PackageArchiveReader[] Packages;
        public Func<PackageArchiveReader, IEnumerable<(string Path, Stream data)>> Selector;

        public override IEnumerable<(string Path, Stream Data)> GetFiles()
        {
            return Packages.Select(Selector).SelectMany(s => s);
        }
    }
}
