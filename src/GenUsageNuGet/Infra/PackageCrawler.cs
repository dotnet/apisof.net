using Microsoft.Cci.Extensions;
using NuGet.Frameworks;
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
            var framework = GetFrameworkFromPackagePath(packagePath);            
            
            await using var assemblyStream = reader.GetStream(packagePath);
            await using var memoryStream = new MemoryStream();
            await assemblyStream.CopyToAsync(memoryStream);

            memoryStream.Position = 0;

            var env = new HostEnvironment();
            var assembly = env.LoadAssemblyFrom(packagePath, memoryStream);
            if (assembly is null)
                continue;

            var assemblyContext = new AssemblyContext {
                Package = reader,
                Framework = framework
            };
            
            collectorSet.Collect(assembly, assemblyContext);
        }

        return collectorSet.GetResults();
    }

    private static NuGetFramework? GetFrameworkFromPackagePath(string packagePath)
    {
        var segments = packagePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length >= 3)
        {
            try
            {
                var frameworkFolder = segments[1];
                var result = NuGetFramework.ParseFolder(frameworkFolder);
                if (result.IsSpecificFramework)
                    return result;
            }
            catch
            {
                // Ignore
            }
        }
        
        return null;
    }
}