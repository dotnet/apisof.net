using System.IO.Compression;
using System.Text.Json;

using Azure.Storage.Blobs;

using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public sealed class CatalogService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    private CatalogJobInfo _jobInfo;
    private ApiCatalogModel _catalog;
    private ApiAvailabilityContext _availabilityContext;
    private SuffixTree _suffixTree;
    private ApiCatalogStatistics _statistics;

    public CatalogService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvalidateAsync()
    {
        if (!_environment.IsDevelopment())
        {
            File.Delete(GetDatabasePath());
            File.Delete(GetSuffixTreePath());
        }

        var azureConnectionString = _configuration["AzureStorageConnectionString"];

        var databasePath = GetDatabasePath();
        if (!File.Exists(databasePath))
        {
            var blobClient = new BlobClient(azureConnectionString, "catalog", "apicatalog.dat");
            await blobClient.DownloadToAsync(databasePath);
        }

        var catalog = await ApiCatalogModel.LoadAsync(databasePath);
        var availabilityContext = ApiAvailabilityContext.Create(catalog);

        var suffixTreePath = GetSuffixTreePath();
        if (!File.Exists(suffixTreePath))
        {
            // TODO: Ideally the underlying file format uses compression. This seems weird.
            var blobClient = new BlobClient(azureConnectionString, "catalog", "suffixtree.dat.deflate");
            using var blobStream = await blobClient.OpenReadAsync();
            using var deflateStream = new DeflateStream(blobStream, CompressionMode.Decompress);
            using var fileStream = File.Create(suffixTreePath);
            await deflateStream.CopyToAsync(fileStream);
        }

        var suffixTree = SuffixTree.Load(suffixTreePath);

        var jobBlobClient = new BlobClient(azureConnectionString, "catalog", "job.json");
        using var jobStream = await jobBlobClient.OpenReadAsync();
        var jobInfo = await JsonSerializer.DeserializeAsync<CatalogJobInfo>(jobStream);

        _catalog = catalog;
        _availabilityContext = availabilityContext;
        _statistics = catalog.GetStatistics();
        _suffixTree = suffixTree;
        _jobInfo = jobInfo;
    }

    private string GetDatabasePath()
    {
        var binDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
        var cacheLocation = Path.Combine(binDirectory, "apicatalog.dat");
        return cacheLocation;
    }

    private string GetSuffixTreePath()
    {
        var databasePath = GetDatabasePath();
        var directory = Path.GetDirectoryName(databasePath);
        var cacheLocation = Path.Combine(directory, "suffixTree.dat");
        return cacheLocation;
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