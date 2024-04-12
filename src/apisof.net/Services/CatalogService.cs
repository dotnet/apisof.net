using System.IO.Compression;
using System.Text.Json;
using ApisOfDotNet.Shared;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.DesignNotes;

namespace ApisOfDotNet.Services;

public sealed class CatalogService
{
    private readonly IOptions<ApisOfDotNetOptions> _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CatalogService> _logger;
    private CatalogData _data = CatalogData.Empty;

    public CatalogService(IOptions<ApisOfDotNetOptions> options,
                          IWebHostEnvironment environment,
                          ILogger<CatalogService> logger)
    {
        ThrowIfNull(options);
        ThrowIfNull(environment);
        ThrowIfNull(logger);

        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvalidateAsync()
    {
        var storageConnectionString = _options.Value.AzureStorageConnectionString;
        var catalogPath = GetCatalogPath();
        var suffixTreePath = GetSuffixTreePath();
        var designNotesPath = GetDesignNotesPath();

        _data = await LoadCatalogDataAsync(_environment, _logger, storageConnectionString, catalogPath, suffixTreePath, designNotesPath);
    }

    private static async Task<CatalogData> LoadCatalogDataAsync(IHostEnvironment environment,
                                                                ILogger logger,
                                                                string storageConnectionString,
                                                                string catalogPath,
                                                                string suffixTreePath,
                                                                string designNotesPath)
    {
        if (!environment.IsDevelopment())
        {
            File.Delete(catalogPath);
            File.Delete(suffixTreePath);
            File.Delete(designNotesPath);
        }

        if (File.Exists(catalogPath))
        {
            logger.LogInformation("Found catalog on disk. Skipping download.");
        }
        else
        {
            logger.LogInformation("Downloading catalog...");

            var blobClient = new BlobClient(storageConnectionString, "catalog", "apicatalog.dat");
            await blobClient.DownloadToAsync(catalogPath);

            logger.LogInformation("Downloading catalog complete.");
        }

        logger.LogInformation("Loading catalog...");

        var catalog = await ApiCatalogModel.LoadAsync(catalogPath);

        logger.LogInformation("Loading catalog complete.");

        if (File.Exists(suffixTreePath))
        {
            logger.LogInformation("Found suffix tree on disk. Skipping download.");
        }
        else
        {
            logger.LogInformation("Downloading suffix tree...");

            // TODO: Ideally the underlying file format uses compression. This seems weird.
            var blobClient = new BlobClient(storageConnectionString, "catalog", "suffixtree.dat.deflate");
            await using var blobStream = await blobClient.OpenReadAsync();
            await using var deflateStream = new DeflateStream(blobStream, CompressionMode.Decompress);
            await using var fileStream = File.Create(suffixTreePath);
            await deflateStream.CopyToAsync(fileStream);

            logger.LogInformation("Download suffix tree complete.");
        }

        logger.LogInformation("Loading suffix tree...");

        var suffixTree = SuffixTree.Load(suffixTreePath);

        logger.LogInformation("Loading suffix tree complete.");

        if (File.Exists(designNotesPath))
        {
            logger.LogInformation("Found design notes on disk. Skipping download.");
        }
        else
        {
            logger.LogInformation("Downloading design notes...");

            var blobClient = new BlobClient(storageConnectionString, "catalog", "designNotes.dat");
            await blobClient.DownloadToAsync(designNotesPath);

            logger.LogInformation("Downloading desing notes complete.");
        }

        logger.LogInformation("Loading design notes...");

        var reviewLinks = DesignNoteDatabase.Load(designNotesPath);

        logger.LogInformation("Loading design notes complete.");

        var jobBlobClient = new BlobClient(storageConnectionString, "catalog", "job.json");
        await using var jobStream = await jobBlobClient.OpenReadAsync();
        var jobInfo = await JsonSerializer.DeserializeAsync<CatalogJobInfo>(jobStream) ?? CatalogJobInfo.Empty;

        return new CatalogData(jobInfo, catalog, suffixTree, reviewLinks);
    }

    private string GetCatalogPath()
    {
        var environmentPath = Environment.GetEnvironmentVariable("APISOFDOTNET_INDEX_PATH");
        var applicationPath = Path.GetDirectoryName(GetType().Assembly.Location)!;
        var directory = environmentPath ?? applicationPath;
        return Path.Combine(directory, "apicatalog.dat");
    }

    private string GetSuffixTreePath()
    {
        var databasePath = GetCatalogPath();
        var directory = Path.GetDirectoryName(databasePath)!;
        return Path.Combine(directory, "suffixTree.dat");
    }

    private string GetDesignNotesPath()
    {
        var databasePath = GetCatalogPath();
        var directory = Path.GetDirectoryName(databasePath)!;
        return Path.Combine(directory, "designNotes.dat");
    }

    public ApiCatalogModel Catalog => _data.Catalog;

    public ApiCatalogStatistics CatalogStatistics => _data.Statistics;

    public CatalogJobInfo JobInfo => _data.JobInfo;

    public DesignNoteDatabase DesignNoteDatabase => _data.DesignNotes;

    public IEnumerable<ApiModel> Search(string query)
    {
        // TODO: Ideally, we'd limit the search results from inside, rather than ToArray()-ing and then limiting.
        // TODO: We should include positions.
        return _data.SuffixTree.Lookup(query)
            .ToArray()
            .Select(t => _data.Catalog.GetApiById(t.Value))
            .Distinct()
            .Take(200);
    }

    private sealed class CatalogData
    {
        public static CatalogData Empty { get; } = new();

        private CatalogData()
            : this(CatalogJobInfo.Empty, ApiCatalogModel.Empty, SuffixTree.Empty, DesignNoteDatabase.Empty)
        {
        }

        public CatalogData(CatalogJobInfo jobInfo, ApiCatalogModel catalog, SuffixTree suffixTree, DesignNoteDatabase designNotes)
        {
            JobInfo = jobInfo;
            Catalog = catalog;
            SuffixTree = suffixTree;
            DesignNotes = designNotes;
            Statistics = catalog.GetStatistics();
        }

        public CatalogJobInfo JobInfo { get; }

        public ApiCatalogModel Catalog { get; }

        public SuffixTree SuffixTree { get; }

        public DesignNoteDatabase DesignNotes { get; }

        public ApiCatalogStatistics Statistics { get; }
    }
}