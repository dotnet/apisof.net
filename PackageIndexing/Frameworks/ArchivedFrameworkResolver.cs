using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PackageIndexing
{
    public sealed class ArchivedFrameworkResolver : FrameworkResolver
    {
        private readonly string _archivePath;

        public ArchivedFrameworkResolver(string archivePath)
        {
            _archivePath = archivePath;
        }

        public override IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve()
        {
            return Directory.GetDirectories(_archivePath)
                            .Select(p => (Path.GetFileName(p), (FileSet)new PathFileSet(Directory.GetFiles(p, "*.dll", SearchOption.AllDirectories))));
        }
    }
}
