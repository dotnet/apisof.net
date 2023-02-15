using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace Terrajobst.NetUpgradePlanner;

internal static class CatalogExtensions
{
    public static string GetLatestNetFramework(this ApiCatalogModel catalog)
    {
        return catalog.Frameworks
                      .Select(f => NuGetFramework.Parse(f.Name))
                      .Where(f => string.Equals(f.Framework, ".NETFramework", StringComparison.OrdinalIgnoreCase))
                      .MaxBy(f => f.Version)!
                      .GetShortFolderName();
    }
}
