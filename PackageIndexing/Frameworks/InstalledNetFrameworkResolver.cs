using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuGet.Frameworks;

namespace PackageIndexing
{
    public sealed class InstalledNetFrameworkResolver : FrameworkResolver
    {
        public static InstalledNetFrameworkResolver Instance = new InstalledNetFrameworkResolver();

        private InstalledNetFrameworkResolver()
        {
        }

        public override IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve()
        {
            var folder = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework";

            foreach (var versionFolder in Directory.GetDirectories(folder))
            {
                var redistXml = Path.Join(versionFolder, "RedistList", "FrameworkList.xml");
                if (File.Exists(redistXml))
                {
                    var version = Path.GetFileName(versionFolder).Replace("v", "");
                    var facadeFoler = Path.Combine(version, "Facades");
                    var framework = NuGetFramework.Parse("net" + version).GetShortFolderName();
                    var files = Directory.GetFiles(versionFolder, "*.dll").ToList();

                    if (Directory.Exists(facadeFoler))
                    {
                        var facadeFiles = Directory.GetFiles(facadeFoler, "*.dll").ToList();
                        files.AddRange(facadeFiles);
                    }

                    var fileSet = new PathFileSet(files);
                    yield return (framework, fileSet);
                }
            }
        }
    }
}
