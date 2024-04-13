using Microsoft.Cci.Extensions;
using NuGet.Packaging.Core;
using Terrajobst.UsageCrawling.Collectors;

namespace GenUsageNuGet.Infra;

public static class PackageCrawler
{
    public static async Task<CollectionSetResults> CrawlAsync(NuGetFeed feed, PackageIdentity packageId)
    {
        var collectorSet = new UsageCollectorSet();
        var reader = await feed.GetPackageAsync(packageId);

        foreach (var packagePath in reader.GetFiles())
        {
            await using var assemblyStream = reader.GetStream(packagePath);
            await using var memoryStream = new MemoryStream();
            await assemblyStream.CopyToAsync(memoryStream);

            memoryStream.Position = 0;

            var env = new HostEnvironment();
            var assembly = env.LoadAssemblyFrom(packagePath, memoryStream);
            if (assembly is null)
                continue;

            collectorSet.Collect(assembly);
        }

        return collectorSet.GetResults();
    }
}