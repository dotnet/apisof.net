using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Frameworks;

namespace ApiCatalog
{
    internal sealed class ArchivedFrameworkLocator : FrameworkLocator
    {
        private readonly string _frameworksPath;

        public ArchivedFrameworkLocator(string frameworksPath)
        {
            _frameworksPath = frameworksPath;
        }

        public override async Task<FileSet> LocateAsync(NuGetFramework framework)
        {
            var path = Path.Combine(_frameworksPath, framework.GetShortFolderName());
            if (!Directory.Exists(path))
                return null;

            var paths = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            if (paths.Length == 0)
                return null;

            return new PathFileSet(paths);
        }
    }
}
