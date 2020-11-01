using System.IO;
using System.Threading.Tasks;

using NuGet.Frameworks;

namespace ApiCatalog
{
    public sealed class PclFrameworkLocator : FrameworkLocator
    {
        private readonly string _archiveFolder;

        public PclFrameworkLocator(string archiveFolder)
        {
            _archiveFolder = archiveFolder;
        }

        public override async Task<FileSet> LocateAsync(NuGetFramework framework)
        {
            var portablePath = Path.Combine(_archiveFolder, framework.Framework);

            if (framework.Framework != ".NETPortable" || !Directory.Exists(portablePath))
                return null;

            var versionDirectories = Directory.GetDirectories(portablePath);

            foreach (var versionDirectory in versionDirectories)
            {
                var profileDirectory = Path.Join(versionDirectory, "Profile", framework.Profile);
                if (Directory.Exists(profileDirectory))
                {
                    var paths = Directory.GetFiles(versionDirectory, "*.dll");
                    return new PathFileSet(paths);
                }
            }

            return null;
        }
    }
}
