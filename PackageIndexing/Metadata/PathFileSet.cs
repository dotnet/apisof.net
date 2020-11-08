using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PackageIndexing
{
    public class PathFileSet : FileSet
    {
        private readonly IReadOnlyList<string> _paths;

        public PathFileSet(IReadOnlyList<string> paths)
        {
            _paths = paths;
        }

        public override IEnumerable<(string Path, Stream Data)> GetFiles()
        {
            return _paths.Select(p => (p, (Stream)File.OpenRead(p)));
        }
    }
}
