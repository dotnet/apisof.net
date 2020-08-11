using System.Collections.Generic;
using System.IO;

namespace ApiCatalog
{
    public abstract class FileSet
    {
        public abstract IEnumerable<(string Path, Stream Data)> GetFiles();
    }
}
