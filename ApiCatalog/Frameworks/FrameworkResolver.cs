using System.Collections.Generic;

namespace ApiCatalog
{
    public abstract class FrameworkResolver
    {
        public abstract IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve();
    }
}
