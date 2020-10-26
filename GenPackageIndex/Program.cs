using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

using ApiCatalog;

using NuGet.Packaging.Core;

namespace GenPackageIndex
{
    internal static class Program
    {
        private static readonly string[] _dotnetPlatformOwners = new[] {
            "aspnet",
            "dotnetframework",
            "EntityFramework",
            "RoslynTeam",
            //"dotnetfoundation"
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

            var httpClient = new HttpClient();

            var feed = new NuGetFeed(sourceUrl);

            Console.Error.WriteLine($"Fetching owner information...");
            var ownerInformation = await feed.GetOwnerMappingAsync();
            timingResults.Add(("Fetching owners", stopwatch.Elapsed));

            // Find all pages in the catalog index.

            Console.Error.WriteLine($"Fetching packages...");
            var packages = await feed.GetAllPackages(createdAfter);
            timingResults.Add(("Fetching packages", stopwatch.Elapsed));

            var filteredPackages = new ConcurrentBag<PackageIdentity>(packages.Where(l => IsOwnedByDotNet(ownerInformation, l.Id)));

            Console.Error.WriteLine($"Processing {filteredPackages.Count:N0} packages...");

            var packageDocument = new XDocument();
            var root = new XElement("packages");
            packageDocument.Add(root);

            foreach (var item in filteredPackages.OrderBy(p => p))
            {
                var e = new XElement("package",
                    new XAttribute("id", item.Id),
                    new XAttribute("version", item.Version)
                );

                root.Add(e);
            }

            packageDocument.Save(@"C:\Users\immo\Downloads\packages\packages.xml");

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
    }
}
