using System.Xml.Linq;

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

        Console.WriteLine($"Fetching packages...");
        var packages = await feed.GetAllPackages();

        var packageDocument = new XDocument();
        var root = new XElement("packages");
        packageDocument.Add(root);

        var filteredPackages = packages.Where(l => IsOwnedByDotNet(ownerInformation, l.Id))
            .OrderBy(p => p);

        foreach (var item in filteredPackages)
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