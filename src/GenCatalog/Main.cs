using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using NuGet.Frameworks;

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
        try
        {
            var rootPath = _pathProvider.RootPath;
            var indexPath = Path.Combine(rootPath, "index");
            var packagesPath = Path.Combine(rootPath, "packages");
            var packageListPath = Path.Combine(packagesPath, "packages.xml");
            var frameworksPath = Path.Combine(rootPath, "frameworks");
            var packsPath = Path.Combine(rootPath, "packs");
            var suffixTreePath = Path.Combine(rootPath, "suffixTree.dat");
            var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");

            var indexStore = new FileSystemIndexStore(indexPath);

            var stopwatch = Stopwatch.StartNew();

            await DownloadArchivedPlatformsAsync(frameworksPath);
            await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
            await DownloadDotnetPackageListAsync(packageListPath);
            await GeneratePlatformIndexAsync(frameworksPath, indexStore);
            await GeneratePackageIndexAsync(packageListPath, packagesPath, frameworksPath, indexStore);
            await GenerateCatalogAsync(indexStore, catalogModelPath);
            await GenerateSuffixTreeAsync(catalogModelPath, suffixTreePath);
            await _store.UploadApiCatalogAsync(catalogModelPath);
            await _store.UploadSuffixTreeAsync(suffixTreePath);

            var catalogSize = new FileInfo(catalogModelPath).Length;
            var suffixTreeSize = new FileInfo(suffixTreePath).Length;
            _summaryTable.AppendBytes("Catalog Size", catalogSize);
            _summaryTable.AppendBytes("Suffix Tree Size", suffixTreeSize);

            await PostToGenCatalogWebHook();

            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");

            await UploadSummaryAsync(success: true);
        }
        catch
        {
            await UploadSummaryAsync(success: false);
            throw;
        }
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

    private static Task GeneratePlatformIndexAsync(string frameworksPath, IndexStore indexStore)
    {
        var frameworkResolvers = new FrameworkProvider[] {
            new ArchivedFrameworkProvider(frameworksPath),
            new PackBasedFrameworkProvider(frameworksPath)
        };

        var frameworks = frameworkResolvers
            .SelectMany(r => r.Resolve())
            .OrderBy(t => t.FrameworkName)
            .Select(t => (Framework: NuGetFramework.Parse(t.FrameworkName), t.Assemblies))
            .GroupBy(t => new NuGetFramework(t.Framework.Framework, t.Framework.Version));

        foreach (var group in frameworks)
        {
            var assemblyByPath = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
            var assemblyEntryByPath = new Dictionary<string, AssemblyEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var (framework, assemblies) in group)
            {
                var frameworkName = framework.GetShortFolderName();
                var alreadyIndexed = indexStore.HasFramework(frameworkName);

                if (alreadyIndexed)
                {
                    Console.WriteLine($"{frameworkName} already indexed.");
                }
                else
                {
                    Console.WriteLine($"Indexing {frameworkName}...");
                    var frameworkEntry = FrameworkIndexer.Index(frameworkName, assemblies, assemblyByPath, assemblyEntryByPath);
                    indexStore.Store(frameworkEntry);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static async Task GeneratePackageIndexAsync(string packageListPath, string packagesPath, string frameworksPath, IndexStore indexStore)
    {
        var frameworkLocators = new FrameworkLocator[] {
            new ArchivedFrameworkLocator(frameworksPath),
            new PackBasedFrameworkLocator(frameworksPath),
            new PclFrameworkLocator(frameworksPath)
        };

        Directory.CreateDirectory(packagesPath);

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
            var retriedCorruptedPackage = false;
            RetryPackage:

            var alreadyIndexed = !retryIndexed && indexStore.IsMarkedAsIndexed(id, version) ||
                                 !retryDisabled && indexStore.IsMarkedAsDisabled(id, version) ||
                                 !retryFailed && indexStore.IsMarkedAsFailed(id, version);

            if (alreadyIndexed)
            {
                if (indexStore.IsMarkedAsIndexed(id, version))
                    Console.WriteLine($"Package {id} {version} already indexed.");

                if (indexStore.IsMarkedAsDisabled(id, version))
                    nugetStore.DeleteFromCache(id, version);
            }
            else
            {
                Console.WriteLine($"Indexing {id} {version}...");
                try
                {
                    if (await nugetStore.IsMarkedAsLegacyAsync(id, version))
                    {
                        Console.WriteLine("Package is marked as legacy.");
                        indexStore.MarkPackageAsDisabled(id, version);
                        nugetStore.DeleteFromCache(id, version);
                    }
                    else
                    {
                        var packageEntry = await packageIndexer.Index(id, version);
                        if (packageEntry is not null)
                        {
                            indexStore.Store(packageEntry);
                        }
                        else
                        {
                            Console.WriteLine($"Not a library package.");
                            indexStore.MarkPackageAsDisabled(id, version);
                            nugetStore.DeleteFromCache(id, version);
                        }
                    }
                }
                catch (InvalidDataException ex) when (!retriedCorruptedPackage)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine("Package appears to be corrupted, trying to download again");
                    retriedCorruptedPackage = true;
                    indexStore.MarkPackageAsNotIndexed(id, version);
                    nugetStore.DeleteFromCache(id, version);
                    goto RetryPackage;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed: {ex}");
                    indexStore.MarkPackageAsFailed(id, version, ex);
                }
            }
        }
    }

    private async Task GenerateCatalogAsync(IndexStore indexStore, string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
        {
            Console.WriteLine($"Skipping generation of {Path.GetFileName(catalogModelPath)}.");
            return;
        }

        File.Delete(catalogModelPath);

        var builder = new CatalogBuilder();
        builder.Index(indexStore);
        builder.Build(catalogModelPath);

        var model = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var stats = model.GetStatistics();
        var statsText = stats.ToString();
        Console.WriteLine("Catalog stats:");
        Console.Write(statsText);
        await File.WriteAllTextAsync(Path.ChangeExtension(catalogModelPath, ".txt"), statsText);

        _summaryTable.AppendBytes("Size Compressed", stats.SizeCompressed);
        _summaryTable.AppendBytes("Size Uncompressed", stats.SizeUncompressed);
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
