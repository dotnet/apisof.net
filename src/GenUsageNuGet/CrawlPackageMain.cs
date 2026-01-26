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

        try
        {
            var results = await PackageCrawler.CrawlAsync(NuGetFeed.NuGetOrg, packageId);
            await results.SaveAsync(fileName);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                                               ex.Message.Contains("404") ||
                                               ex.Message.Contains("specified blob does not exist", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Warning: Package {packageId.Id} {packageId.Version} not found (404). Skipping.");
            throw;
        }
        catch (StackOverflowException)
        {
            Console.WriteLine($"Error: Stack overflow while processing {packageId.Id} {packageId.Version}. Skipping this package.");
            throw;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"Warning: Task cancelled for {packageId.Id} {packageId.Version}. Timeout or cancellation occurred.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error crawling package {packageId.Id} {packageId.Version}: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}