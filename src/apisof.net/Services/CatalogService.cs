using System.IO.Compression;
using System.Text.Json;

using Azure.Storage.Blobs;

using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public sealed class CatalogService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CatalogService> _logger;

    private CatalogJobInfo _jobInfo;
    private ApiCatalogModel _catalog;
    private ApiAvailabilityContext _availabilityContext;
    private SuffixTree _suffixTree;
    private ApiCatalogStatistics _statistics;

    public CatalogService(IConfiguration configuration,
                          IWebHostEnvironment environment,
                          ILogger<CatalogService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvalidateAsync()
    {
        if (!_environment.IsDevelopment())
        {
            File.Delete(GetCatalogPath());
            File.Delete(GetSuffixTreePath());
        }

        var azureConnectionString = _configuration["AzureStorageConnectionString"];

        var databasePath = GetCatalogPath();
        if (File.Exists(databasePath))
        {
            _logger.LogInformation("Found catalog on disk. Skipping download.");
        }
        else
        {
            _logger.LogInformation("Downloading catalog...");

            var blobClient = new BlobClient(azureConnectionString, "catalog", "apicatalog.dat");
            await blobClient.DownloadToAsync(databasePath);

            _logger.LogInformation("Downloading catalog complete.");
        }

        _logger.LogInformation("Loading catalog...");

        var catalog = await ApiCatalogModel.LoadAsync(databasePath);
        var availabilityContext = ApiAvailabilityContext.Create(catalog);

        _logger.LogInformation("Loading catalog complete.");

        var suffixTreePath = GetSuffixTreePath();
        if (File.Exists(suffixTreePath))
        {
            _logger.LogInformation("Found suffix tree on disk. Skipping download.");
        }
        else
        {
            _logger.LogInformation("Downloading suffix tree...");

            // TODO: Ideally the underlying file format uses compression. This seems weird.
            var blobClient = new BlobClient(azureConnectionString, "catalog", "suffixtree.dat.deflate");
            using var blobStream = await blobClient.OpenReadAsync();
            using var deflateStream = new DeflateStream(blobStream, CompressionMode.Decompress);
            using var fileStream = File.Create(suffixTreePath);
            await deflateStream.CopyToAsync(fileStream);

            _logger.LogInformation("Download suffix tree complete.");
        }

        _logger.LogInformation("Loading suffix tree...");

        var suffixTree = SuffixTree.Load(suffixTreePath);

        _logger.LogInformation("Loading suffix tree complete.");

        var jobBlobClient = new BlobClient(azureConnectionString, "catalog", "job.json");
        using var jobStream = await jobBlobClient.OpenReadAsync();
        var jobInfo = await JsonSerializer.DeserializeAsync<CatalogJobInfo>(jobStream);

        _catalog = catalog;
        _availabilityContext = availabilityContext;
        _statistics = catalog.GetStatistics();
        _suffixTree = suffixTree;
        _jobInfo = jobInfo;
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

    public ApiCatalogModel Catalog => _catalog;

    public ApiAvailabilityContext AvailabilityContext => _availabilityContext;

    public ApiCatalogStatistics CatalogStatistics => _statistics;

    public CatalogJobInfo JobInfo => _jobInfo;

    public IEnumerable<ApiModel> Search(string query)
    {
        // TODO: Ideally, we'd limit the search results from inside, rather than ToArray()-ing and then limiting.
        // TODO: We should include positions.
        return _suffixTree.Lookup(query)
            .ToArray()
            .Select(t => _catalog.GetApiById(t.Value))
            .Distinct()
            .Take(200);
    }
}