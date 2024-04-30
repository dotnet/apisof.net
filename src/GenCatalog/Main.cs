using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.ActionsRunner;

namespace GenCatalog;

internal sealed class Main : IConsoleMain
{
    private readonly ApisOfDotNetPathProvider _pathProvider;
    private readonly ApisOfDotNetStore _store;
    private readonly ApisOfDotNetWebHook _webHook;
    private readonly GitHubActionsEnvironment _gitHubActionsEnvironment;
    private readonly GitHubActionsSummaryTable _summaryTable;

    public Main(ApisOfDotNetPathProvider pathProvider,
                ApisOfDotNetStore store,
                ApisOfDotNetWebHook webHook,
                GitHubActionsEnvironment gitHubActionsEnvironment,
                GitHubActionsSummaryTable summaryTable)
    {
        ThrowIfNull(pathProvider);
        ThrowIfNull(store);
        ThrowIfNull(webHook);

        _pathProvider = pathProvider;
        _store = store;
        _webHook = webHook;
        _gitHubActionsEnvironment = gitHubActionsEnvironment;
        _summaryTable = summaryTable;
    }

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var success = true;
        try
        {
            var rootPath = _pathProvider.RootPath;
            var indexPath = Path.Combine(rootPath, "index");
            var indexFrameworksPath = Path.Combine(indexPath, "frameworks");
            var indexPackagesPath = Path.Combine(indexPath, "packages");
            var packagesPath = Path.Combine(rootPath, "packages");
            var packageListPath = Path.Combine(packagesPath, "packages.xml");
            var frameworksPath = Path.Combine(rootPath, "frameworks");
            var packsPath = Path.Combine(rootPath, "packs");
            var suffixTreePath = Path.Combine(rootPath, "suffixTree.dat");
            var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");

            var stopwatch = Stopwatch.StartNew();

            await DownloadArchivedPlatformsAsync(frameworksPath);
            await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
            await DownloadDotnetPackageListAsync(packageListPath);
            await GeneratePlatformIndexAsync(frameworksPath, indexFrameworksPath);
            await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath, frameworksPath);
            await GenerateCatalogAsync(indexFrameworksPath, indexPackagesPath, catalogModelPath);
            await GenerateSuffixTreeAsync(catalogModelPath, suffixTreePath);
            await _store.UploadApiCatalogAsync(catalogModelPath);
            await _store.UploadSuffixTreeAsync(suffixTreePath);

            var catalogSize = new FileInfo(catalogModelPath).Length;
            var suffixTreeSize = new FileInfo(suffixTreePath).Length;
            _summaryTable.AppendBytes("Catalog Size", catalogSize);
            _summaryTable.AppendBytes("Suffix Tree Size", suffixTreeSize);
            
            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            success = false;
        }

        await UploadSummaryAsync(success);

        if (success)
            await PostToGenCatalogWebHook();
    }

    private async Task DownloadArchivedPlatformsAsync(string archivePath)
    {
        var container = "archive";

        foreach (var name in await _store.GetBlobNamesAsync(container))
        {
            var nameWithoutExtension = Path.ChangeExtension(name, null);
            var localDirectory = Path.Combine(archivePath, nameWithoutExtension);
            if (!Directory.Exists(localDirectory))
            {
                using var blobStream = await _store.OpenReadAsync(container, name);
                using var archive = new ZipArchive(blobStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(localDirectory);
            }
        }
        _summaryTable.AppendNumber("#Archived Frameworks", Directory.GetDirectories(archivePath).Length);
        
    }

    private async Task DownloadPackagedPlatformsAsync(string archivePath, string packsPath)
    {
        var frameworkCount = await FrameworkDownloader.DownloadAsync(archivePath, packsPath);
        _summaryTable.AppendNumber("#Packaged Frameworks", frameworkCount);
    }

    private async Task DownloadDotnetPackageListAsync(string packageListPath)
    {
        if (File.Exists(packageListPath))
        {
            Console.WriteLine($"Skipping download of {Path.GetFileName(packageListPath)}.");
            return;
        }

        var packageCount = await DotnetPackageIndex.CreateAsync(packageListPath);
        _summaryTable.AppendNumber("#Packages", packageCount);
    }

    private static Task GeneratePlatformIndexAsync(string frameworksPath, string indexFrameworksPath)
    {
        var frameworkResolvers = new FrameworkProvider[] {
            new ArchivedFrameworkProvider(frameworksPath),
            new PackBasedFrameworkProvider(frameworksPath)
        };

        var frameworks = frameworkResolvers.SelectMany(r => r.Resolve())
            .OrderBy(t => t.FrameworkName);
        var reindex = false;

        Directory.CreateDirectory(indexFrameworksPath);

        foreach (var (frameworkName, paths) in frameworks)
        {
            var path = Path.Join(indexFrameworksPath, $"{frameworkName}.xml");
            var alreadyIndexed = !reindex && File.Exists(path);

            if (alreadyIndexed)
            {
                Console.WriteLine($"{frameworkName} already indexed.");
            }
            else
            {
                Console.WriteLine($"Indexing {frameworkName}...");
                var frameworkEntry = FrameworkIndexer.Index(frameworkName, paths);
                using (var stream = File.Create(path))
                    frameworkEntry.Write(stream);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task GeneratePackageIndexAsync(string packageListPath, string packagesPath, string indexPackagesPath, string frameworksPath)
    {
        var frameworkLocators = new FrameworkLocator[] {
            new ArchivedFrameworkLocator(frameworksPath),
            new PackBasedFrameworkLocator(frameworksPath),
            new PclFrameworkLocator(frameworksPath)
        };

        Directory.CreateDirectory(packagesPath);
        Directory.CreateDirectory(indexPackagesPath);

        var document = XDocument.Load(packageListPath);
        Directory.CreateDirectory(packagesPath);

        var packages = document.Root!.Elements("package")
            .Select(e => (
                Id: e.Attribute("id")!.Value,
                Version: e.Attribute("version")!.Value))
            .ToArray();

        var nightlies = new NuGetFeed(NuGetFeeds.NightlyLatest);
        var nuGetOrg = new NuGetFeed(NuGetFeeds.NuGetOrg);
        var nugetStore = new NuGetStore(packagesPath, nightlies, nuGetOrg);
        var packageIndexer = new PackageIndexer(nugetStore, frameworkLocators);

        var retryIndexed = false;
        var retryDisabled = false;
        var retryFailed = false;

        foreach (var (id, version) in packages)
        {
            var path = Path.Join(indexPackagesPath, $"{id}-{version}.xml");
            var disabledPath = Path.Join(indexPackagesPath, $"{id}-all.disabled");
            var failedVersionPath = Path.Join(indexPackagesPath, $"{id}-{version}.failed");

            var alreadyIndexed = !retryIndexed && File.Exists(path) ||
                                 !retryDisabled && File.Exists(disabledPath) ||
                                 !retryFailed && File.Exists(failedVersionPath);

            if (alreadyIndexed)
            {
                if (File.Exists(path))
                    Console.WriteLine($"Package {id} {version} already indexed.");

                if (File.Exists(disabledPath))
                    nugetStore.DeleteFromCache(id, version);
            }
            else
            {
                Console.WriteLine($"Indexing {id} {version}...");
                try
                {
                    var packageEntry = await packageIndexer.Index(id, version);
                    if (packageEntry is null)
                    {
                        Console.WriteLine($"Not a library package.");
                        File.WriteAllText(disabledPath, string.Empty);
                        nugetStore.DeleteFromCache(id, version);
                    }
                    else
                    {
                        using (var stream = File.Create(path))
                            packageEntry.Write(stream);

                        File.Delete(disabledPath);
                        File.Delete(failedVersionPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed: {ex}");
                    File.Delete(disabledPath);
                    File.Delete(path);
                    File.WriteAllText(failedVersionPath, ex.ToString());
                }
            }
        }
    }

    private async Task GenerateCatalogAsync(string platformsPath, string packagesPath, string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
        {
            Console.WriteLine($"Skipping generation of {Path.GetFileName(catalogModelPath)}.");
            return;
        }

        File.Delete(catalogModelPath);

        var builder = new CatalogBuilder();
        builder.Index(platformsPath);
        builder.Index(packagesPath);
        builder.Build(catalogModelPath);

        var model = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var stats = model.GetStatistics();
        var statsText = stats.ToString();
        Console.WriteLine("Catalog stats:");
        Console.Write(statsText);
        await File.WriteAllTextAsync(Path.ChangeExtension(catalogModelPath, ".txt"), statsText);

        _summaryTable.AppendNumber("Size Compressed", stats.SizeCompressed);
        _summaryTable.AppendNumber("Size Uncompressed", stats.SizeUncompressed);
        _summaryTable.AppendNumber("#APIs", stats.NumberOfApis);
        _summaryTable.AppendNumber("#Extension Methods", stats.NumberOfExtensionMethods);
        _summaryTable.AppendNumber("#Declarations", stats.NumberOfDeclarations);
        _summaryTable.AppendNumber("#Assemblies", stats.NumberOfAssemblies);
        _summaryTable.AppendNumber("#Frameworks", stats.NumberOfFrameworks);
        _summaryTable.AppendNumber("#Framework Assemblies", stats.NumberOfFrameworkAssemblies);
        _summaryTable.AppendNumber("#Packages", stats.NumberOfPackages);
        _summaryTable.AppendNumber("#Package Versions", stats.NumberOfPackageVersions);
        _summaryTable.AppendNumber("#Package Assemblies", stats.NumberOfPackageAssemblies);

        foreach (var row in stats.TableSizes)
        {
            _summaryTable.AppendBytes($"{row.TableName} Size", row.Bytes);

            if (row.Rows >= 0)
                _summaryTable.AppendNumber($"{row.TableName} #Rows", row.Rows);
        }
    }

    private static async Task GenerateSuffixTreeAsync(string catalogModelPath, string suffixTreePath)
    {
        if (File.Exists(suffixTreePath))
            return;

        Console.WriteLine($"Generating {Path.GetFileName(suffixTreePath)}...");
        var catalog = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var builder = new SuffixTreeBuilder();

        foreach (var api in catalog.AllApis)
        {
            if (api.Kind.IsAccessor())
                continue;

            builder.Add(api.ToString(), api.Id);
        }

        await using var stream = File.Create(suffixTreePath);
        builder.WriteSuffixTree(stream);
    }

    private Task PostToGenCatalogWebHook()
    {
        return _webHook.InvokeAsync(ApisOfDotNetWebHookSubject.ApiCatalog);
    }

    private async Task UploadSummaryAsync(bool success)
    {
        var job = new Job {
            Date = DateTimeOffset.UtcNow,
            Success = success,
            DetailsUrl = _gitHubActionsEnvironment.RunUrl
        };

        using var jobStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(jobStream, job);
        jobStream.Position = 0;

        await _store.UploadAsync("catalog", "job.json", jobStream);
    }

    internal sealed class Job
    {
        public DateTimeOffset Date { get; set; }
        public bool Success { get; set; }
        public string? DetailsUrl { get; set; }
    }
}