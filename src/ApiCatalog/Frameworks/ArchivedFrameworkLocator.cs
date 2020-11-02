using System.IO;
using System.Threading.Tasks;

using NuGet.Frameworks;

namespace ApiCatalog
{
    public sealed class ArchivedFrameworkLocator : FrameworkLocator
    {
        private readonly string _frameworksPath;

        public ArchivedFrameworkLocator(string frameworksPath)
        {
            _frameworksPath = frameworksPath;
        }

        public override async Task<FileSet> LocateAsync(NuGetFramework framework)
        {
            var shortFolderName = GetFolderName(framework);
            var path = Path.Combine(_frameworksPath, shortFolderName);
            if (!Directory.Exists(path))
                return null;

            var paths = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            if (paths.Length == 0)
                return null;

            return new PathFileSet(paths);
        }

        private string GetFolderName(NuGetFramework framework)
        {
            // Special case Xamarin platforms

            if (framework.Framework == FrameworkConstants.FrameworkIdentifiers.MonoAndroid)
                return "monoandroid";

            if (framework.Framework == FrameworkConstants.FrameworkIdentifiers.XamarinIOs ||
                framework.Framework == FrameworkConstants.FrameworkIdentifiers.MonoTouch)
                return "xamarinios";

            if (framework.Framework == FrameworkConstants.FrameworkIdentifiers.XamarinMac ||
                framework.Framework == FrameworkConstants.FrameworkIdentifiers.MonoMac)
                return "xamarinmac";

            if (framework.Framework == FrameworkConstants.FrameworkIdentifiers.XamarinWatchOS)
                return "xamarinwatchos";

            if (framework.Framework == FrameworkConstants.FrameworkIdentifiers.XamarinTVOS)
                return "xamarintvos";

            return framework.GetShortFolderName();
        }
    }
}
