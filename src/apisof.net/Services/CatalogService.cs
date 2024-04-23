using System.IO.Compression;
using ApisOfDotNet.Shared;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.DesignNotes;
using Terrajobst.ApiCatalog.Features;

namespace ApisOfDotNet.Services;

public sealed class CatalogService
{
    private readonly IOptions<ApisOfDotNetOptions> _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CatalogService> _logger;

    private readonly BlobSource<ApiCatalogModel> _catalogBlobSource;
    private readonly BlobSource<SuffixTree> _suffixTreeBlobSource;
    private readonly BlobSource<CatalogJobInfo> _catalogJobBlobSource;
    private readonly BlobSource<DesignNoteDatabase> _designNotesBlobSource;
    private readonly BlobSource<FeatureUsageData> _usageBlobSource;
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

        _catalogBlobSource = CreateBlobSource("catalog", "apicatalog.dat", ApiCatalogModel.LoadAsync);
        _suffixTreeBlobSource = CreateBlobSource("catalog", "suffixtree.dat.deflate", SuffixTree.Load);
        _catalogJobBlobSource = CreateBlobSource("catalog", "job.json", CatalogJobInfo.Load);
        _designNotesBlobSource = CreateBlobSource("catalog", "designNotes.dat", DesignNoteDatabase.Load);
        _usageBlobSource = CreateBlobSource("usage", "usageData.dat", FeatureUsageData.Load);
    }

    public async Task InvalidateAsync()
    {
        var invalidateCachedDownload = !_environment.IsDevelopment();
        var catalog = await _catalogBlobSource.DownloadAsync(invalidateCachedDownload);
        var suffixTree = await _suffixTreeBlobSource.DownloadAsync(invalidateCachedDownload);
        var jobInfo = await _catalogJobBlobSource.DownloadAsync(invalidateCachedDownload);
        var usageData = await _usageBlobSource.DownloadAsync(invalidateCachedDownload);
        var designNotes = DesignNoteDatabase.Load(_designNotesBlobSource.GetLocalPath());
        _data = new CatalogData(catalog, suffixTree, jobInfo, usageData, designNotes);
    }

    private BlobSource<T> CreateBlobSource<T>(string containerName, string blobName, Func<string, T> loader)
    {
        return new BlobSource<T>(_logger, _options, containerName, blobName, s => Task.FromResult(loader(s)));
    }

    private BlobSource<T> CreateBlobSource<T>(string containerName, string blobName, Func<string, Task<T>> loader)
    {
        return new BlobSource<T>(_logger, _options, containerName, blobName, loader);
    }

    public ApiCatalogModel Catalog => _data.Catalog;

    public FeatureUsageData UsageData => _data.UsageData;

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

    private abstract class BlobSource
    {
        protected BlobSource(string containerName,
                             string blobName)
        {
            ThrowIfNullOrEmpty(containerName);
            ThrowIfNullOrEmpty(blobName);

            ContainerName = containerName;
            BlobName = blobName;
        }

        public string ContainerName { get; }

        public string BlobName { get; }

        public string GetLocalPath()
        {
            var environmentPath = Environment.GetEnvironmentVariable("APISOFDOTNET_INDEX_PATH");
            var applicationPath = Path.GetDirectoryName(GetType().Assembly.Location)!;
            var directory = environmentPath ?? applicationPath;
            return Path.Combine(directory, BlobName);
        }
    }

    private sealed class BlobSource<T> : BlobSource
    {
        private readonly ILogger<CatalogService> _logger;
        private readonly IOptions<ApisOfDotNetOptions> _options;
        private readonly Func<string, Task<T>> _loader;

        public BlobSource(ILogger<CatalogService> logger,
                          IOptions<ApisOfDotNetOptions> options,
                          string containerName,
                          string blobName,
                          Func<string, Task<T>> loader)
            : base(containerName, blobName)
        {
            ThrowIfNull(logger);
            ThrowIfNull(options);
            ThrowIfNull(loader);

            _logger = logger;
            _options = options;
            _loader = loader;
        }

        public async Task<T> DownloadAsync(bool invalidateCachedDownload)
        {
            var localPath = GetLocalPath();

            if (!invalidateCachedDownload && File.Exists(localPath))
            {
                _logger.LogInformation($"Found {BlobName}. Skipping download.");
            }
            else
            {
                _logger.LogInformation($"Downloading {BlobName}...");

                var blobClient = new BlobClient(_options.Value.AzureStorageConnectionString, ContainerName, BlobName);

                if (!this.BlobName.EndsWith(".deflate", StringComparison.OrdinalIgnoreCase))
                {
                    await blobClient.DownloadToAsync(localPath);
                }
                else
                {
                    await using var blobStream = await blobClient.OpenReadAsync();
                    await using var deflateStream = new DeflateStream(blobStream, CompressionMode.Decompress);
                    await using var fileStream = File.Create(localPath);
                    await deflateStream.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"Downloading {BlobName} complete.");
            }

            return await _loader(localPath);
        }
    }

    private sealed class CatalogData
    {
        public static CatalogData Empty { get; } = new();

        private CatalogData()
            : this(ApiCatalogModel.Empty, SuffixTree.Empty, CatalogJobInfo.Empty, FeatureUsageData.Empty, DesignNoteDatabase.Empty)
        {
        }

        public CatalogData(ApiCatalogModel catalog, SuffixTree suffixTree, CatalogJobInfo jobInfo, FeatureUsageData usageData, DesignNoteDatabase designNotes)
        {
            ThrowIfNull(catalog);
            ThrowIfNull(suffixTree);
            ThrowIfNull(jobInfo);
            ThrowIfNull(usageData);
            ThrowIfNull(designNotes);

            Catalog = catalog;
            SuffixTree = suffixTree;
            JobInfo = jobInfo;
            UsageData = usageData;
            DesignNotes = designNotes;
            Statistics = catalog.GetStatistics();
        }

        public ApiCatalogModel Catalog { get; }

        public SuffixTree SuffixTree { get; }

        public CatalogJobInfo JobInfo { get; }

        public FeatureUsageData UsageData { get; }

        public DesignNoteDatabase DesignNotes { get; }

        public ApiCatalogStatistics Statistics { get; }

        public CatalogData WithCatalog(ApiCatalogModel catalog, SuffixTree suffixTree, CatalogJobInfo jobInfo)
        {
            ThrowIfNull(catalog);
            ThrowIfNull(suffixTree);
            ThrowIfNull(jobInfo);

            if (ReferenceEquals(catalog, Catalog) &&
                ReferenceEquals(suffixTree, SuffixTree) &&
                ReferenceEquals(jobInfo, JobInfo))
                return this;

            return new CatalogData(catalog, suffixTree, jobInfo, UsageData, DesignNotes);
        }

        public CatalogData WithUsageData(FeatureUsageData usageData)
        {
            ThrowIfNull(usageData);

            if (ReferenceEquals(usageData, UsageData))
                return this;

            return new CatalogData(Catalog, SuffixTree, JobInfo, usageData, DesignNotes);
        }

        public CatalogData WithDesignNotes(DesignNoteDatabase designNotes)
        {
            ThrowIfNull(designNotes);

            if (ReferenceEquals(designNotes, DesignNotes))
                return this;

            return new CatalogData(Catalog, SuffixTree, JobInfo, UsageData, designNotes);
        }
    }

    public async void HandleBlobChange(string blobPath)
    {
        if (string.IsNullOrEmpty(blobPath))
            return;

        _logger.LogInformation($"Received blob change for {blobPath}.");

        if (blobPath.EndsWith("job.json", StringComparison.OrdinalIgnoreCase))
            await ReloadCatalogAsync();
        else if (blobPath.EndsWith("designNotes.dat", StringComparison.OrdinalIgnoreCase))
            await ReloadDesignNotesAsync();
        else if (blobPath.EndsWith("usageData.dat", StringComparison.OrdinalIgnoreCase))
            await ReloadUsageDataAsync();
    }

    private async Task ReloadCatalogAsync()
    {
        _logger.LogInformation("Reloading catalog...");

        const bool invalidateCachedDownload = true;
        var catalog = await _catalogBlobSource.DownloadAsync(invalidateCachedDownload);
        var suffixTree = await _suffixTreeBlobSource.DownloadAsync(invalidateCachedDownload);
        var jobInfo = await _catalogJobBlobSource.DownloadAsync(invalidateCachedDownload);
        _data = _data.WithCatalog(catalog, suffixTree, jobInfo);
    }

    private async Task ReloadDesignNotesAsync()
    {
        _logger.LogInformation("Reloading design notes...");

        const bool invalidateCachedDownload = true;
        var designNotes = await _designNotesBlobSource.DownloadAsync(invalidateCachedDownload);
        _data = _data.WithDesignNotes(designNotes);
    }

    private async Task ReloadUsageDataAsync()
    {
        _logger.LogInformation("Reloading usage data...");

        const bool invalidateCachedDownload = true;
        var usageData = await _usageBlobSource.DownloadAsync(invalidateCachedDownload);
        _data = _data.WithUsageData(usageData);
    }
}