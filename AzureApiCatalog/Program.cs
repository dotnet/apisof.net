using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Queues;

using Microsoft.Extensions.Configuration.UserSecrets;

using Newtonsoft.Json;

using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace AzureApiCatalog
{
    internal static class Program
    {
        private const int MaxDegreeOfParallelism = 64;
        private static readonly string v3_flatContainer_nupkg_template = "https://api.nuget.org/v3-flatcontainer/{0}/{1}/{0}.{1}.nupkg";

        private static readonly string[] _dotnetPlatformOwners = new[] {
            "aspnet",
            "dotnetframework",
            "EntityFramework",
            "RoslynTeam"
        };

        private static async Task Main(string[] args)
        {
            var rightNumberOfArgs = args.Length > 0 && args.Length < 3;

            if (!rightNumberOfArgs)
            {
                Console.Error.WriteLine("Missing arguments: <service index url> <optional: created after>");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Examples:");
                Console.Error.WriteLine();
                Console.Error.WriteLine("app.exe https://apidev.nugettest.org/v3/index.json");
                Console.Error.WriteLine("app.exe https://apiint.nugettest.org/v3/index.json");
                Console.Error.WriteLine("app.exe https://api.nuget.org/v3/index.json 8/8/18");
                return;
            }

            var sourceUrl = args[0];
            var createdAfter = (args.Length == 2) ? (DateTime?)DateTime.Parse(args[1]) : null;

            var timingResults = new List<(string, TimeSpan)>();
            var stopwatch = Stopwatch.StartNew();

            // Prepare the processing.
            ThreadPool.SetMinThreads(MaxDegreeOfParallelism, completionPortThreads: 4);
            ServicePointManager.DefaultConnectionLimit = MaxDegreeOfParallelism;
            ServicePointManager.MaxServicePointIdleTime = 10000;

            var handler = new HttpClientHandler();
            handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            var httpClient = new HttpClient(handler);

            Console.Error.WriteLine($"Fetching owner information...");
            var ownerInformation = await GetOwnerInformation(httpClient);
            timingResults.Add(("Fetching owners", stopwatch.Elapsed));

            // Discover the catalog index URL from the service index.
            Console.Error.WriteLine($"Discovering index URL...");
            var catalogIndexUrl = await GetCatalogIndexUrlAsync(sourceUrl);
            timingResults.Add(("Getting catalog", stopwatch.Elapsed));

            // Download the catalog index and deserialize it.
            Console.Error.WriteLine($"Fetching catalog index {catalogIndexUrl}...");
            var indexString = await httpClient.GetStringAsync(catalogIndexUrl);
            timingResults.Add(("Fetching index", stopwatch.Elapsed));

            Console.Error.WriteLine($"Fetched catalog index {catalogIndexUrl}, fetching catalog pages...");
            var index = JsonConvert.DeserializeObject<CatalogIndex>(indexString);

            // Find all pages in the catalog index.
            var pageItems = new ConcurrentBag<CatalogPage>(index.Items);
            var allLeafItemsBag = new ConcurrentBag<CatalogLeaf>();

            var fetchLeafsTasks = RunInParallel(async () =>
            {
                while (pageItems.TryTake(out var pageItem))
                {
                    if (createdAfter != null && pageItem.CommitTimeStamp < createdAfter.Value)
                        continue;

                    // Download the catalog page and deserialize it.
                    var pageString = await httpClient.GetStringAsync(pageItem.Url);
                    var page = JsonConvert.DeserializeObject<CatalogPage>(pageString);

                    var pageLeafItems = page.Items;

                    foreach (var pageLeafItem in page.Items)
                    {
                        if (pageLeafItem.Type == "nuget:PackageDetails")
                            allLeafItemsBag.Add(pageLeafItem);
                    }
                }
            });

            await Task.WhenAll(fetchLeafsTasks);
            Console.Error.WriteLine($"Fetched {index.Items.Count:N0} catalog pages, finding catalog leaves...");
            timingResults.Add(("Fetching pages", stopwatch.Elapsed));

            var filteredLeaves = allLeafItemsBag.AsEnumerable()
                                                .Where(l => IsOwnedByDotNet(ownerInformation, l.Id));

            if (createdAfter != null)
                filteredLeaves = filteredLeaves.Where(l => l.CommitTimeStamp >= createdAfter.Value);

            var filteredLeafGroups = filteredLeaves
                .GroupBy(l => new PackageIdentity(l.Id, NuGetVersion.Parse(l.Version)))
                .Select(g => g.OrderByDescending(l => l.CommitTimeStamp).First())
                .ToArray();

            var filteredLeafItems = new ConcurrentBag<CatalogLeaf>(filteredLeafGroups);
            timingResults.Add(("Filtering leaves", stopwatch.Elapsed));

            // Process all of the catalog leaf items.
            Console.Error.WriteLine($"Processing {filteredLeafItems.Count:N0} catalog leaves...");

            // Instantiate a QueueClient which will be used to create and manipulate the queue
            var userSecret = UserSecrets.Load();

            var processTasks = RunInParallel(async () =>
            {
                var queueClient = new QueueClient(userSecret.QueueConnectionString, "package-queue");
                await queueClient.CreateIfNotExistsAsync();

                while (filteredLeafItems.TryTake(out var leaf))
                {
                    var message = new PackageQueueMessage
                    {
                        PackageId = leaf.Id,
                        PackageVersion = leaf.Version
                    };

                    var json = JsonConvert.SerializeObject(message);
                    // See https://github.com/Azure/azure-sdk-for-net/issues/10242
                    // Why we're doing base 64 here.
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    var jsonBase64 = Convert.ToBase64String(jsonBytes);
                    await queueClient.SendMessageAsync(jsonBase64);
                }
            });

            await Task.WhenAll(processTasks);

            timingResults.Add(("Queuing work", stopwatch.Elapsed));

            var rolling = TimeSpan.Zero;

            foreach (var (label, duration) in timingResults)
            {
                var relative = duration - rolling;
                rolling += relative;

                Console.WriteLine($"{label,-20}: {relative}");
            }

            Console.WriteLine($"{"Total",-20}: {rolling}");
        }

        private static bool IsOwnedByDotNet(Dictionary<string, string[]> ownerInformation, string id)
        {
            if (ownerInformation.TryGetValue(id, out var owners))
            {
                foreach (var owner in owners)
                {
                    foreach (var platformOwner in _dotnetPlatformOwners)
                    {
                        if (string.Equals(owner, platformOwner, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        private static Task<Dictionary<string, string[]>> GetOwnerInformation(HttpClient httpClient)
        {
            var url = "https://nugetprodusncazuresearch.blob.core.windows.net/v3-azuresearch-014/owners/owners.v2.json";
            return httpClient.GetFromJsonAsync<Dictionary<string, string[]>>(url);
        }

        private static async Task<Uri> GetCatalogIndexUrlAsync(string sourceUrl)
        {
            // This code uses the NuGet client SDK, which are the libraries used internally by the official
            // NuGet client.
            var sourceRepository = Repository.Factory.GetCoreV3(sourceUrl);
            var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            var catalogIndexUrl = serviceIndex.GetServiceEntryUri("Catalog/3.0.0");
            return catalogIndexUrl;
        }

        private static List<Task> RunInParallel(Func<Task> work)
        {
            return Enumerable
                .Range(0, MaxDegreeOfParallelism)
                .Select(i => work())
                .ToList();
        }

        private static async Task ProcessTask(CatalogLeaf message)
        {
            var httpClient = new HttpClient();
            var url = GetFlatContainerNupkgUrl(message);

            using (var nupkgStream = await httpClient.GetStreamAsync(url))
            using (var packageArchiveReader = new PackageArchiveReader(nupkgStream))
            {
                var nuspecReader = packageArchiveReader.NuspecReader;

                var owner = nuspecReader.GetOwners();
            }
        }

        private static Uri GetFlatContainerNupkgUrl(CatalogLeaf message)
        {
            var url = string.Format(v3_flatContainer_nupkg_template,
                    message.Id,
                    message.Version);

            return new Uri(url);
        }
    }

    internal class UserSecrets
    {
        public string QueueConnectionString { get; set; }

        public static UserSecrets Load()
        {
            var path = PathHelper.GetSecretsPathFromSecretsId("a65cd530-6c72-4fa1-a7d6-002260365e65");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UserSecrets>(json);
        }
    }

    public abstract class CatalogEntity
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        [JsonProperty("commitTimeStamp")]
        public DateTime CommitTimeStamp { get; set; }
    }

    public class CatalogIndex : CatalogEntity
    {
        public List<CatalogPage> Items { get; set; }
    }

    public class CatalogPage : CatalogEntity
    {
        public List<CatalogLeaf> Items { get; set; }
    }

    public class CatalogLeaf : CatalogEntity
    {
        [JsonProperty("nuget:id")]
        public string Id { get; set; }

        [JsonProperty("nuget:version")]
        public string Version { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }
    }

    public class Cursor
    {
        [JsonProperty("value")]
        public DateTime Value { get; set; }
    }

    //----------------------

    public class PackageQueueMessage
    {
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
    }
}
