using System.IO.Compression;
using System.Xml.Linq;

using Newtonsoft.Json;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public sealed class FrameworkDefinition
{
    public static IReadOnlyList<FrameworkDefinition> All = new List<FrameworkDefinition>
    {
        new()
        {
            FrameworkName = "netcoreapp3.0",
            FrameworkReferences =
            {
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.NETCore.App.Ref",
                    TargetingPackVersion = "3.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.AspNetCore.App",
                    TargetingPackName = "Microsoft.AspNetCore.App.Ref",
                    TargetingPackVersion = "3.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" },
                    ImplicitViaSdk = "Microsoft.NET.Sdk.Web"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "3.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WPF",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "3.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" },
                    Profile = "WPF",
                    ImplicitViaSdk = "Microsoft.NET.Sdk.WindowsDesktop",
                    ImplicitViaProperty = "UseWPF"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WindowsForms",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "3.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" },
                    Profile = "WindowsForms",
                    ImplicitViaSdk = "Microsoft.NET.Sdk.WindowsDesktop",
                    ImplicitViaProperty = "UseWindowsForms"
                }
            }
        },
        new()
        {
            FrameworkName = "netcoreapp3.1",
            FrameworkReferences =
            {
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.NETCore.App.Ref",
                    TargetingPackVersion = "3.1",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.AspNetCore.App",
                    TargetingPackName = "Microsoft.AspNetCore.App.Ref",
                    TargetingPackVersion = "3.1",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" },
                    ImplicitViaSdk = "Microsoft.NET.Sdk.Web"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "3.1",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WPF",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "3.1",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" },
                    Profile = "WPF",
                    ImplicitViaSdk = "Microsoft.NET.Sdk.WindowsDesktop",
                    ImplicitViaProperty = "UseWPF"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WindowsForms",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "3.1",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "" },
                    Profile = "WindowsForms",
                    ImplicitViaSdk = "Microsoft.NET.Sdk.WindowsDesktop",
                    ImplicitViaProperty = "UseWindowsForms"
                }
            }
        },
        new()
        {
            FrameworkName = "net5.0",
            FrameworkReferences =
            {
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.NETCore.App.Ref",
                    TargetingPackVersion = "5.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "", "windows" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.AspNetCore.App",
                    TargetingPackName = "Microsoft.AspNetCore.App.Ref",
                    TargetingPackVersion = "5.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "", "windows" },
                    ImplicitViaSdk = "Microsoft.NET.Sdk.Web"
                },
                //
                // Windows
                //
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "5.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "windows" },
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WPF",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "5.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "windows" },
                    Profile = "WPF",
                    ImplicitViaProperty = "UseWPF"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WindowsForms",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "5.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "windows" },
                    Profile = "WindowsForms",
                    ImplicitViaProperty = "UseWindowsForms"
                }
            }
        },
        new()
        {
            FrameworkName = "net6.0",
            FrameworkReferences =
            {
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.NETCore.App.Ref",
                    TargetingPackVersion = "6.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "", "windows", "android", "ios", "macos", "tvos", "watchos" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.AspNetCore.App",
                    TargetingPackName = "Microsoft.AspNetCore.App.Ref",
                    TargetingPackVersion = "6.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "", "windows" },
                    ImplicitViaSdk = "Microsoft.NET.Sdk.Web"
                },
                //
                // Windows
                //
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "6.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "windows" },
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WPF",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "6.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "windows" },
                    Profile = "WPF",
                    ImplicitViaProperty = "UseWPF"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WindowsForms",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "6.0",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "windows" },
                    Profile = "WindowsForms",
                    ImplicitViaProperty = "UseWindowsForms"
                },
                //
                // Android
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.Android.Ref",
                    TargetingPackVersion = "11",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "android" }
                },
                //
                // iOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.iOS.Ref",
                    TargetingPackVersion = "14",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "ios" }
                },
                //
                // macOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.macOS.Ref",
                    TargetingPackVersion = "15",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "macos" }
                },
                //
                // tvOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.tvOS.Ref",
                    TargetingPackVersion = "14",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "tvos" }
                },
                //
                // watchOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.watchOS.Ref",
                    TargetingPackVersion = "7",
                    NuGetFeed = NuGetFeeds.NuGetOrg,
                    Platforms = { "tvos" }
                },
            }
        },
        new()
        {
            FrameworkName = "net7.0",
            FrameworkReferences =
            {
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.NETCore.App.Ref",
                    TargetingPackVersion = "7.0",
                    NuGetFeed = NuGetFeeds.NightlyDotnet7,
                    Platforms = { "", "windows", "android", "ios", "macos", "tvos", "watchos" }
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.AspNetCore.App",
                    TargetingPackName = "Microsoft.AspNetCore.App.Ref",
                    TargetingPackVersion = "7.0",
                    NuGetFeed = NuGetFeeds.NightlyDotnet7,
                    Platforms = { "", "windows" },
                    ImplicitViaSdk = "Microsoft.NET.Sdk.Web"
                },
                //
                // Windows
                //
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "7.0",
                    NuGetFeed = NuGetFeeds.NightlyDotnet7,
                    Platforms = { "windows" },
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WPF",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "7.0",
                    NuGetFeed = NuGetFeeds.NightlyDotnet7,
                    Platforms = { "windows" },
                    Profile = "WPF",
                    ImplicitViaProperty = "UseWPF"
                },
                new FrameworkReferenceDefinition
                {
                    Name = "Microsoft.WindowsDesktop.App.WindowsForms",
                    TargetingPackName = "Microsoft.WindowsDesktop.App.Ref",
                    TargetingPackVersion = "7.0",
                    NuGetFeed = NuGetFeeds.NightlyDotnet7,
                    Platforms = { "windows" },
                    Profile = "WindowsForms",
                    ImplicitViaProperty = "UseWindowsForms"
                },
                //
                // Android
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.Android.Ref",
                    TargetingPackVersion = "11",
                    NuGetFeed = NuGetFeeds.NightlyXamarin,
                    Platforms = { "android" }
                },
                //
                // iOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.iOS.Ref",
                    TargetingPackVersion = "14",
                    NuGetFeed = NuGetFeeds.NightlyXamarin,
                    Platforms = { "ios" }
                },
                //
                // macOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.macOS.Ref",
                    TargetingPackVersion = "15",
                    NuGetFeed = NuGetFeeds.NightlyXamarin,
                    Platforms = { "macos" }
                },
                //
                // tvOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.tvOS.Ref",
                    TargetingPackVersion = "14",
                    NuGetFeed = NuGetFeeds.NightlyXamarin,
                    Platforms = { "tvos" }
                },
                //
                // watchOS
                //
                new FrameworkReferenceDefinition
                {
                    TargetingPackName = "Microsoft.watchOS.Ref",
                    TargetingPackVersion = "7",
                    NuGetFeed = NuGetFeeds.NightlyXamarin,
                    Platforms = { "tvos" }
                },
            }
        }
    };

    public string FrameworkName { get; set; }
    public List<FrameworkReferenceDefinition> FrameworkReferences { get; } = new();
}

public sealed class FrameworkReferenceDefinition
{
    public List<string> Platforms { get; } = new();
    public string Name { get; set; }
    public string NuGetFeed { get; set; }
    public string TargetingPackName { get; set; }
    public string TargetingPackVersion { get; set; }
    public string Profile { get; set; }
    public string ImplicitViaSdk { get; set; }
    public string ImplicitViaProperty { get; set; }
}

public static class FrameworkPackIndex
{
    public static string FileName => "packIndex.json";

    public static void Save(IReadOnlyList<FrameworkPackIndexEntry> entries, string path)
    {
        var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public static IReadOnlyList<FrameworkPackIndexEntry> Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<IReadOnlyList<FrameworkPackIndexEntry>>(json);
    }
}

public sealed class FrameworkPackIndexEntry
{
    public string FrameworkName { get; set; }
    public string FrameworkReference { get; set; }
    public string ImplicitViaSdk { get; set; }
    public string ImplicitViaProperty { get; set; }
    public string[] AssemblyPaths { get; set; }
}

public sealed class FrameworkDownloader
{
    public static async Task Download(string frameworksPath, string packsPath)
    {
        foreach (var frameworkDefinition in FrameworkDefinition.All)
        {
            await Download(frameworksPath, packsPath, frameworkDefinition);
        }
    }

    private static async Task Download(string frameworksPath, string packsPath, FrameworkDefinition frameworkDefinition)
    {
        var frameworkName = NuGetFramework.Parse(frameworkDefinition.FrameworkName);
        var majorVersion = frameworkName.Version.Major;
        var entries = new List<FrameworkPackIndexEntry>();
        var frameworkPath = Path.Combine(frameworksPath, frameworkDefinition.FrameworkName);
        if (Directory.Exists(frameworkPath))
            return;

        Console.WriteLine($"Downloading {frameworkDefinition.FrameworkName}...");
        Directory.CreateDirectory(frameworkPath);

        foreach (var frameworkReference in frameworkDefinition.FrameworkReferences)
        {
            var feed = new NuGetFeed(frameworkReference.NuGetFeed);
            var range = VersionRange.Parse($"(,{frameworkReference.TargetingPackVersion}]");

            var versions = await feed.GetAllVersionsAsync(frameworkReference.TargetingPackName);
            var latest = versions.Where(v => range.Satisfies(v)).DefaultIfEmpty().Max();
            if (latest == null)
                continue;

            var identity = new PackageIdentity(frameworkReference.TargetingPackName, latest);
            var packDirectoryPath = Path.Combine(packsPath, $"{identity.Id}_{identity.Version}");

            if (!Directory.Exists(packDirectoryPath))
            {
                Console.WriteLine($"Downloading {identity}...");

                using var memoryStream = new MemoryStream();
                await feed.CopyPackageStreamAsync(identity, memoryStream);
                memoryStream.Position = 0;
                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(packDirectoryPath);
            }

            var frameworkListPath = Path.Combine(packDirectoryPath, "data", "FrameworkList.xml");
            var frameworkList = XDocument.Load(frameworkListPath);
            var paths = new List<string>();

            foreach (var node in frameworkList.Descendants("File"))
            {
                var type = node.Attribute("Type")?.Value;
                var relativePath = node.Attribute("Path").Value;
                if (type is not null && type != "Managed")
                    continue;
                if (!relativePath.StartsWith("ref/"))
                    relativePath = $"ref/{frameworkDefinition.FrameworkName}/{relativePath}";
                var path = Path.GetRelativePath(frameworkPath, Path.Combine(packDirectoryPath, relativePath));
                var profileList = node.Attribute("Profile")?.Value ?? string.Empty;
                var profiles = profileList.Split(';').Select(p => p.Trim()).ToList();
                if (frameworkReference.Profile == null || profiles.Contains(frameworkReference.Profile, StringComparer.OrdinalIgnoreCase))
                    paths.Add(path);
            }

            var tfms = frameworkReference.Platforms == null
                ? new[] { frameworkDefinition.FrameworkName }
                : frameworkReference.Platforms.Select(p => string.IsNullOrEmpty(p) ? frameworkDefinition.FrameworkName : $"{frameworkDefinition.FrameworkName}-{p}").ToArray();

            foreach (var tfm in tfms)
            {
                var entry = new FrameworkPackIndexEntry
                {
                    FrameworkName = tfm,
                    FrameworkReference = frameworkReference.Name,
                    ImplicitViaSdk = frameworkReference.ImplicitViaSdk,
                    ImplicitViaProperty = frameworkReference.ImplicitViaProperty,
                    AssemblyPaths = paths.ToArray(),
                };
                entries.Add(entry);
            }
        }

        var packIndexPath = Path.Combine(frameworkPath, FrameworkPackIndex.FileName);
        FrameworkPackIndex.Save(entries, packIndexPath);
    }
}