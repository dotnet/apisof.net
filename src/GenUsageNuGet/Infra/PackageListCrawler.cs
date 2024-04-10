using NuGet.Packaging.Core;

namespace GenUsageNuGet.Infra;

public abstract class PackageListCrawler
{
    public abstract Task<IReadOnlyList<PackageIdentity>> GetPackagesAsync();
}