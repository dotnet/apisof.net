using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuGet.Frameworks;

namespace ApiCatalog
{
    public sealed class ArchivedFrameworkProvider : FrameworkProvider
    {
        private readonly string _frameworksPath;

        public ArchivedFrameworkProvider(string frameworksPath)
        {
            _frameworksPath = frameworksPath;
        }

        public override IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve()
        {
            return Directory.GetDirectories(_frameworksPath)
                            .Where(p => !NuGetFramework.Parse(Path.GetFileName(p)).IsUnsupported)
                            .Select(p => (Path.GetFileName(p), (FileSet)new PathFileSet(Directory.GetFiles(p, "*.dll", SearchOption.AllDirectories))))
                            .Where(t => t.Item2.GetFiles().Any());
        }
    }
}
