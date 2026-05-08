using System.IO.Compression;
using ApisOfDotNet.Shared;
using Azure.Storage.Blobs;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.DesignNotes;
using Terrajobst.ApiCatalog.Features;

namespace ApisOfDotNet.Services;

public sealed class CatalogService
{
    private readonly BlobStorageService _blobStorageService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CatalogService> _logger;

    private readonly BlobSource<ApiCatalogModel> _catalogBlobSource;
    private readonly BlobSource<SuffixTree> _suffixTreeBlobSource;
    private readonly BlobSource<CatalogJobInfo> _catalogJobBlobSource;
    private readonly BlobSource<DesignNoteDatabase> _designNotesBlobSource;
    private readonly BlobSource<FeatureUsageData> _usageBlobSource;
    private CatalogData _data = CatalogData.Empty;

    public CatalogService(BlobStorageService blobStorageService,
                          IWebHostEnvironment environment,
                          ILogger<CatalogService> logger)
    {
        ThrowIfNull(blobStorageService);
        ThrowIfNull(environment);
        ThrowIfNull(logger);

        _blobStorageService = blobStorageService;
        _environment = environment;
        _logger = logger;

        _catalogBlobSource = CreateBlobSource("catalog", "apicatalog.dat", ApiCatalogModel.LoadAsync);
        _suffixTreeBlobSource = CreateBlobSource("catalog", "suffixtree.dat.deflate", SuffixTree.LoadDeflate);
        _catalogJobBlobSource = CreateBlobSource("catalog", "job.json", CatalogJobInfo.Load);
        _designNotesBlobSource = CreateBlobSource("catalog", "designNotes.dat", DesignNoteDatabase.Load);
        _usageBlobSource = CreateBlobSource("usage", "usageData.dat", FeatureUsageData.Load);
    }

    public async Task InvalidateAsync()
    {
        var invalidateCachedDownload = !_environment.IsDevelopment();
        _logger.LogInformation("InvalidateAsync started. Environment={Environment}, InvalidateCachedDownload={InvalidateCache}",
            _environment.EnvironmentName, invalidateCachedDownload);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var catalogTask = _catalogBlobSource.DownloadAsync(invalidateCachedDownload);
        var suffixTreeTask = _suffixTreeBlobSource.DownloadAsync(invalidateCachedDownload);
        var jobInfoTask = _catalogJobBlobSource.DownloadAsync(invalidateCachedDownload);
        var usageDataTask = DownloadWithFallbackAsync(_usageBlobSource, FeatureUsageData.Empty, invalidateCachedDownload);
        var designNotesTask = DownloadWithFallbackAsync(_designNotesBlobSource, DesignNoteDatabase.Empty, invalidateCachedDownload);

        try
        {
            await Task.WhenAll(catalogTask,
                               suffixTreeTask,
                               jobInfoTask,
                               usageDataTask,
                               designNotesTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "One or more downloads failed during InvalidateAsync. " +
                "Catalog={CatalogStatus}, SuffixTree={SuffixTreeStatus}, JobInfo={JobInfoStatus}, UsageData={UsageStatus}, DesignNotes={DesignNotesStatus}",
                catalogTask.Status, suffixTreeTask.Status, jobInfoTask.Status, usageDataTask.Status, designNotesTask.Status);
            throw;
        }

        _data = new CatalogData(catalogTask.Result, suffixTreeTask.Result, jobInfoTask.Result, usageDataTask.Result, designNotesTask.Result);
        _logger.LogInformation("InvalidateAsync completed in {ElapsedMs}ms.", sw.ElapsedMilliseconds);
    }

    private async Task<T> DownloadWithFallbackAsync<T>(BlobSource<T> source, T fallback, bool invalidateCachedDownload)
    {
        try
        {
            return await source.DownloadAsync(invalidateCachedDownload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {BlobName}. Using empty fallback.", source.BlobName);
            return fallback;
        }
    }

    public async void InvalidateCatalog()
    {
        await ReloadCatalogAsync();
    }

    public async void InvalidateDesignNotes()
    {
        await ReloadDesignNotesAsync();
    }

    public async void InvalidateUsageData()
    {
        await ReloadUsageDataAsync();
    }

    private BlobSource<T> CreateBlobSource<T>(string containerName, string blobName, Func<string, T> loader)
    {
        return new BlobSource<T>(_logger, _blobStorageService, containerName, blobName, s => Task.FromResult(loader(s)));
    }

    private BlobSource<T> CreateBlobSource<T>(string containerName, string blobName, Func<string, Task<T>> loader)
    {
        return new BlobSource<T>(_logger, _blobStorageService, containerName, blobName, loader);
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

        protected string GetLocalPath()
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
        private readonly BlobStorageService _blobStorageService;
        private readonly Func<string, Task<T>> _loader;

        public BlobSource(ILogger<CatalogService> logger,
                          BlobStorageService blobStorageService,
                          string containerName,
                          string blobName,
                          Func<string, Task<T>> loader)
            : base(containerName, blobName)
        {
            ThrowIfNull(logger);
            ThrowIfNull(blobStorageService);
            ThrowIfNull(loader);

            _logger = logger;
            _blobStorageService = blobStorageService;
            _loader = loader;
        }

        public async Task<T> DownloadAsync(bool invalidateCachedDownload)
        {
            var localPath = GetLocalPath();
            _logger.LogInformation("DownloadAsync for {BlobName}: localPath={LocalPath}, invalidateCache={InvalidateCache}, fileExists={FileExists}",
                BlobName, localPath, invalidateCachedDownload, File.Exists(localPath));

            if (!invalidateCachedDownload && File.Exists(localPath))
            {
                _logger.LogInformation("Found {BlobName}. Skipping download.", BlobName);
            }
            else
            {
                _logger.LogInformation("Downloading {BlobName} from {ContainerName}...", BlobName, ContainerName);

                await Task.Run(() =>
                {
                    var blobClient = _blobStorageService.GetBlobClient(ContainerName, BlobName);
                    _logger.LogInformation("BlobClient URI for {BlobName}: {BlobUri}", BlobName, blobClient.Uri);
                    return blobClient.DownloadToAsync(localPath);
                });

                var fileInfo = new FileInfo(localPath);
                _logger.LogInformation("Downloaded {BlobName} complete. File size: {FileSize} bytes", BlobName, fileInfo.Exists ? fileInfo.Length : -1);
            }

            var result = await Task.Run(() => _loader(localPath));
            _logger.LogInformation("Loaded {BlobName}.", BlobName);

            return result;
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