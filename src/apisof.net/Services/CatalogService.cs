using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using ApiCatalog;
using ApiCatalog.CatalogModel;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ApiCatalog.Services
{
    public sealed class CatalogService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        private CatalogJobInfo _jobInfo;
        private ApiCatalogModel _catalog;
        private SuffixTree _suffixTree;
        private Dictionary<Guid, ApiModel> _apiByGuid;
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

            var catalog = ApiCatalogModel.Load(databasePath);
            var apiByGuid = catalog.GetAllApis().ToDictionary(a => a.Guid);

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
            _statistics = catalog.GetStatistics();
            _apiByGuid = apiByGuid;
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

        public ApiCatalogStatistics CatalogStatistics => _statistics;

        public CatalogJobInfo JobInfo => _jobInfo;

        public ApiModel GetApiByGuid(Guid guid)
        {
            _apiByGuid.TryGetValue(guid, out var result);
            return result;
        }

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
}
