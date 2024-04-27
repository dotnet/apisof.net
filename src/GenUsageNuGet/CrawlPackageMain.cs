using GenUsageNuGet.Infra;
using Mono.Options;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Terrajobst.ApiCatalog.ActionsRunner;

namespace GenUsageNuGet;

internal sealed class CrawlPackageMain : ConsoleCommand
{
    private string _packageId = "";
    private NuGetVersion _packageVersion = new(0, 0, 0);
    private string _outputPath = "";

    public override string Name => "crawl-package";

    public override string Description => "Crawls a single package";

    public override void AddOptions(OptionSet options)
    {
        options.Add("n=", "{Name} of the package to crawl", v => _packageId = v);
        options.Add("v=", "{Version} of the package to crawl", v => _packageVersion = NuGetVersion.Parse(v));
        options.Add("o=", "{Path} to the output file", v => _outputPath = v);
    }

    public override async Task ExecuteAsync()
    {
        var packageIdentity = new PackageIdentity(_packageId, _packageVersion);
        await CrawlPackageAsync(packageIdentity, _outputPath);
    }

    private static async Task CrawlPackageAsync(PackageIdentity packageId, string fileName)
    {
        Console.WriteLine($"Crawling {packageId}...");

        var results = await PackageCrawler.CrawlAsync(NuGetFeed.NuGetOrg, packageId);
        await results.SaveAsync(fileName);
    }
}