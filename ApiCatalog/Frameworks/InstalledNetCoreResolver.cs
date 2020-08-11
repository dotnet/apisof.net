using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApiCatalog
{
    public sealed class InstalledNetCoreResolver : FrameworkResolver
    {
        public static InstalledNetCoreResolver Instance = new InstalledNetCoreResolver();

        public override IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve()
        {
            var packsFolder = @"C:\Program Files\dotnet\packs\";

            var packs = new List<(string packName, Version packVersion, string frameworkName, string path)>();

            if (Directory.Exists(packsFolder))
            {
                var packDirectories = Directory.GetDirectories(packsFolder);
                foreach (var packDirectory in packDirectories)
                {
                    var packName = Path.GetFileName(packDirectory);
                    var versionDirectories = Directory.GetDirectories(packDirectory);

                    foreach (var versionDirectory in versionDirectories)
                    {
                        var versionName = Path.GetFileName(versionDirectory);

                        if (!Version.TryParse(versionName, out var packVersion))
                            continue;

                        var refFolder = Path.Join(versionDirectory, "ref");
                        if (!Directory.Exists(refFolder))
                            continue;

                        var frameworkFolders = Directory.GetDirectories(refFolder);
                        foreach (var frameworkFolder in frameworkFolders)
                        {
                            var frameworkName = Path.GetFileName(frameworkFolder);
                            packs.Add((packName, packVersion, frameworkName, frameworkFolder));
                        }
                    }
                }
            }

            var latestPackSets = packs.GroupBy(p => (p.packName, p.frameworkName))
                                      .Select(g => g.OrderByDescending(gi => gi.packVersion).First())
                                      .GroupBy(p => p.frameworkName);

            foreach (var packSet in latestPackSets)
            {
                var files = packSet.Select(p => p.path)
                                   .SelectMany(d => Directory.GetFiles(d, "*.dll"))
                                   .ToArray();
                var fileSet = new PathFileSet(files);
                yield return (packSet.Key, fileSet);
            }
        }
    }
}
