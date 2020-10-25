using System.Collections.Generic;
using System.Threading.Tasks;

using NuGet.Frameworks;

namespace ApiCatalog
{
    internal sealed class PackBasedFrameworkLocator : FrameworkLocator
    {
        private readonly string _frameworksPath;
        private Dictionary<string, FileSet> _mappings;

        public PackBasedFrameworkLocator(string frameworksPath)
        {
            _frameworksPath = frameworksPath;
        }

        public override async Task<FileSet> LocateAsync(NuGetFramework framework)
        {
            if (_mappings == null)
            {
                _mappings = new Dictionary<string, FileSet>();
                var provider = new PackBasedFrameworkProvider(_frameworksPath);
                foreach (var (tfm, pathSet) in provider.Resolve())
                    _mappings.Add(tfm, pathSet);
            }

            var key = framework.GetShortFolderName();
            _mappings.TryGetValue(key, out var fileSet);
            return fileSet;
        }
    }
}
