using System.IO;
using System.Threading.Tasks;

using NuGet.Frameworks;

namespace ApiCatalog
{
    internal sealed class ArchivedFrameworkLocator : FrameworkLocator
    {
        private readonly string _archiveFolder;

        public ArchivedFrameworkLocator(string archiveFolder)
        {
            _archiveFolder = archiveFolder;
        }

        public override async Task<FileSet> LocateAsync(NuGetFramework framework)
        {
            var path = Path.Combine(_archiveFolder, framework.GetShortFolderName());
            if (!Directory.Exists(path))
                return null;

            return new PathFileSet(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
        }
    }
}
