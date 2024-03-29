﻿using Microsoft.Cci.Extensions;
using NuGet.Packaging.Core;

namespace Terrajobst.UsageCrawling;

public static class PackageCrawler
{
    public static async Task<CrawlerResults> CrawlAsync(NuGetFeed feed, PackageIdentity packageId)
    {
        var crawler = new AssemblyCrawler();
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

            crawler.Crawl(assembly);
        }

        return crawler.GetResults();
    }
}