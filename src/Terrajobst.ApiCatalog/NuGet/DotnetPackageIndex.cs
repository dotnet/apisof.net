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
        "dotnetiot",
        "EntityFramework",
        "RoslynTeam",
        "nugetsqltools",
        //"dotnetfoundation",
        "newtonsoft",
        "xamarin",
        "corewcf"
    };

    public static async Task CreateAsync(string packageListPath)
    {
        var packages = await GetPackagesAsync(NuGetFeeds.NuGetOrg, NuGetFeeds.NightlyLatest);

        var packageDocument = new XDocument();
        var root = new XElement("packages");
        packageDocument.Add(root);

        foreach (var item in packages)
        {
            var e = new XElement("package",
                new XAttribute("id", item.Identity.Id),
                new XAttribute("version", item.Identity.Version),
                new XAttribute("feed", item.Feed.FeedUrl)
            );

            root.Add(e);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(packageListPath));
        packageDocument.Save(packageListPath);
    }

    private static async Task<IReadOnlyList<PackageIdentityWithFeed>> GetPackagesAsync(params string[] feedUrls)
    {
        var packages = new List<PackageIdentityWithFeed>();

        foreach (var feedUrl in feedUrls)
        {
            var feed = new NuGetFeed(feedUrl);
            var feedPackages = await GetPackagesAsync(feed);
            packages.AddRange(feedPackages);
        }

        Console.WriteLine($"Found {packages.Count:N0} package versions across {feedUrls.Length} feeds.");

        var latestVersions = GetLatestVersions(packages);

        Console.WriteLine($"Found {latestVersions.Count:N0} latest package versions.");

        return latestVersions;
    }

    private static async Task<IReadOnlyList<PackageIdentityWithFeed>> GetPackagesAsync(NuGetFeed feed)
    {
        Console.WriteLine($"Getting packages from {feed.FeedUrl}...");

        if (feed.FeedUrl == NuGetFeeds.NuGetOrg)
            return await GetPackagesFromNuGetOrgAsync(feed);
        else
            return await GetPackagesFromOtherGalleryAsync(feed);
    }

    private static async Task<IReadOnlyList<PackageIdentityWithFeed>> GetPackagesFromNuGetOrgAsync(NuGetFeed feed)
    {
        Console.WriteLine($"Fetching owner information...");
        var ownerInformation = await feed.GetOwnerMappingAsync();

        var packageIds = ownerInformation.Keys
                                         .ToHashSet(StringComparer.OrdinalIgnoreCase)
                                         .Where(id => IsOwnedByDotNet(ownerInformation, id) &&
                                                      PackageFilter.Default.IsMatch(id))
                                         .ToArray();

        Console.WriteLine($"Found {packageIds.Length:N0} relevant package IDs.");

        Console.WriteLine($"Getting versions...");

        ConcurrentBag<PackageIdentity> identities = new ConcurrentBag<PackageIdentity>();

        await Parallel.ForEachAsync(packageIds, async (packageId, _) =>
        {
            var versions = await feed.GetAllVersionsAsync(packageId);

            foreach (var version in versions)
            {
                var identity = new PackageIdentity(packageId, version);
                identities.Add(identity);
            }
        });

        Console.WriteLine($"Found {identities.Count:N0} package versions.");

        return identities.Select(i => new PackageIdentityWithFeed(i, feed)).ToArray();
    }

    private static async Task<IReadOnlyList<PackageIdentityWithFeed>> GetPackagesFromOtherGalleryAsync(NuGetFeed feed)
    {
        Console.WriteLine($"Enumerating feed...");

        var identities = await feed.GetAllPackagesAsync();

        identities = identities.Where(i => PackageFilter.Default.IsMatch(i.Id)).ToArray();

        Console.WriteLine($"Found {identities.Count:N0} package versions.");

        return identities.Select(i => new PackageIdentityWithFeed(i, feed)).ToArray();
    }

    private static IReadOnlyList<PackageIdentityWithFeed> GetLatestVersions(IReadOnlyList<PackageIdentityWithFeed> identities)
    {
        var result = new List<PackageIdentityWithFeed>();

        var groups = identities.GroupBy(i => i.Identity.Id);

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            var packageId = group.Key;
            var versions = group.OrderByDescending(p => p.Identity.Version, VersionComparer.VersionReleaseMetadata);

            var latestStable = versions.FirstOrDefault(i => !i.Identity.Version.IsPrerelease);
            var latestPrerelease = versions.FirstOrDefault(i => i.Identity.Version.IsPrerelease);

            if (latestStable != default && latestPrerelease != default)
            {
                var stableIsNewer = VersionComparer.VersionReleaseMetadata.Compare(latestPrerelease.Identity.Version, latestStable.Identity.Version) <= 0;
                if (stableIsNewer)
                    latestPrerelease = default;
            }

            if (latestStable != default)
                result.Add(latestStable);

            if (latestPrerelease != default)
                result.Add(latestPrerelease);
        }

        return result;
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

    private record struct PackageIdentityWithFeed(PackageIdentity Identity, NuGetFeed Feed);
}