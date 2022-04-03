using System.Collections.Generic;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class PackBasedFrameworkLocator : FrameworkLocator
{
    private readonly string _frameworksPath;
    private Dictionary<string, string[]> _mappings;

    public PackBasedFrameworkLocator(string frameworksPath)
    {
        _frameworksPath = frameworksPath;
    }

    public override string[] Locate(NuGetFramework framework)
    {
        if (_mappings == null)
        {
            _mappings = new Dictionary<string, string[]>();
            var provider = new PackBasedFrameworkProvider(_frameworksPath);
            foreach (var (tfm, paths) in provider.Resolve())
                _mappings.Add(tfm, paths);
        }

        var key = framework.GetShortFolderName();
        _mappings.TryGetValue(key, out var mappingPaths);
        return mappingPaths;
    }
}