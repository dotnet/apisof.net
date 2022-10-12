using NuGet.Frameworks;

using System;
using System.Collections.Generic;
using System.Linq;

using Terrajobst.ApiCatalog;

namespace NetUpgradePlanner.Analysis;

internal static class CatalogExtensions
{
    public static HashSet<string> GetKnownFrameworkAssemblies(this ApiCatalogModel catalog)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in catalog.Assemblies)
            result.Add(assembly.Name);

        return result;
    }

    public static string GetLatestNetFramework(this ApiCatalogModel catalog)
    {
        return catalog.Frameworks
                      .Select(f => NuGetFramework.Parse(f.Name))
                      .Where(f => string.Equals(f.Framework, ".NETFramework", StringComparison.OrdinalIgnoreCase))
                      .MaxBy(f => f.Version)!
                      .GetShortFolderName();
    }
}
