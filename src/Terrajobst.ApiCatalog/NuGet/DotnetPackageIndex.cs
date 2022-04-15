using System.Collections.Concurrent;
using System.Xml.Linq;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public static class DotnetPackageIndex
{
    private static readonly string[] DotnetPlatformOwners = new[] {
        "aspnet",
        "dotnetframework",
        "EntityFramework",
        "RoslynTeam",
        "nugetsqltools"
        //"dotnetfoundation"
    };

    public static async Task CreateAsync(string packageListPath)
    {
        var feed = new NuGetFeed(NuGetFeeds.NuGetOrg);

        Console.WriteLine($"Fetching owner information...");
        var ownerInformation = await feed.GetOwnerMappingAsync();

        var packageDocument = new XDocument();
        var root = new XElement("packages");
        packageDocument.Add(root);

        var packageIds = ownerInformation.Keys
                                         .ToHashSet(StringComparer.OrdinalIgnoreCase)
                                         .Where(id => IsOwnedByDotNet(ownerInformation, id) &&
                                                      PackageFilter.Default.IsMatch(id))
                                         .ToArray();

        Console.WriteLine($"Found {packageIds.Length:N0} relevant platform package IDs.");

        Console.WriteLine($"Filtering to latest versions...");

        var filteredPackages = new ConcurrentBag<PackageIdentity>();

        await Parallel.ForEachAsync(packageIds, async (packageId, _) =>
        {
            var versions = await feed.GetAllVersionsAsync(packageId);
            var identities = versions.Select(v => new PackageIdentity(packageId, v))
                                     .OrderByDescending(v => v.Version, VersionComparer.VersionReleaseMetadata)
                                     .ToArray();

            var latestStable = identities.FirstOrDefault(i => !i.Version.IsPrerelease);
            var latestPrerelease = identities.FirstOrDefault(i => i.Version.IsPrerelease);

            if (latestStable is not null && latestPrerelease is not null)
            {
                var stableIsNewer = VersionComparer.VersionReleaseMetadata.Compare(latestPrerelease.Version, latestStable.Version) <= 0;
                if (stableIsNewer)
                    latestPrerelease = null;
            }

            if (latestStable is not null)
                filteredPackages.Add(latestStable);

            if (latestPrerelease is not null)
                filteredPackages.Add(latestPrerelease);
        });
        
        Console.WriteLine($"Found {filteredPackages.Count:N0} platform package versions.");

        foreach (var item in filteredPackages.OrderBy(pi => pi.Id)
                                             .ThenBy(pi => pi.Version, VersionComparer.VersionReleaseMetadata))
        {
            var e = new XElement("package",
                new XAttribute("id", item.Id),
                new XAttribute("version", item.Version)
            );

            root.Add(e);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(packageListPath));
        packageDocument.Save(packageListPath);
    }

    private static bool IsOwnedByDotNet(Dictionary<string, string[]> ownerInformation, string id)
    {
        if (ownerInformation.TryGetValue(id, out var owners))
        {
            foreach (var owner in owners)
            {
                foreach (var platformOwner in DotnetPlatformOwners)
                {
                    if (string.Equals(owner, platformOwner, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        return false;
    }
}