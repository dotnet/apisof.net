using Terrajobst.ApiCatalog;

namespace Terrajobst.NetUpgradePlanner;

internal static class CatalogExtensions
{
    public static string GetLatestNetFramework(this ApiCatalogModel catalog)
    {
        return catalog.Frameworks
                      .Select(f => f.NuGetFramework)
                      .Where(f => string.Equals(f.Framework, ".NETFramework", StringComparison.OrdinalIgnoreCase))
                      .MaxBy(f => f.Version)!
                      .GetShortFolderName();
    }
}
