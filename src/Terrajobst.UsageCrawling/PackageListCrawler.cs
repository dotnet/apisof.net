using NuGet.Packaging.Core;

namespace Terrajobst.UsageCrawling;

public abstract class PackageListCrawler
{
    public abstract Task<IReadOnlyList<PackageIdentity>> GetPackagesAsync();
}