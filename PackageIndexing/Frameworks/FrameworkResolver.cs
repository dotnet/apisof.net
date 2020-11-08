using System.Collections.Generic;

namespace PackageIndexing
{
    public abstract class FrameworkResolver
    {
        public abstract IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve();
    }
}
