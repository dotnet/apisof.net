using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NuGet.Packaging.Core;

namespace ApiCatalog
{
    public sealed class FrameworkDownloader
    {
        private static (string Feed, string Id, string Tfm)[] _packageMappings = new[]
        {
            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0"),

            // Don't add ASP.NET Core. The fact that there is a shared framework is an implementation detail.
            // Only when building for web, you don't need to add the package references.
            //
            //(WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.AspNetCore.App.Ref",     "net5.0"),

            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0-windows"),
            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.WindowsDesktop.App.Ref", "net5.0-windows"),

            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0-android"),
            (WellKnownNuGetFeeds.NightlyXamarin, "Microsoft.Android.Ref",            "net5.0-android"),

            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0-ios"),
            (WellKnownNuGetFeeds.NightlyXamarin, "Microsoft.iOS.Ref",                "net5.0-ios"),

            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0-macos"),
            (WellKnownNuGetFeeds.NightlyXamarin, "Microsoft.macOS.Ref",              "net5.0-macos"),

            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0-tvos"),
            (WellKnownNuGetFeeds.NightlyXamarin, "Microsoft.tvOS.Ref",               "net5.0-tvos"),

            (WellKnownNuGetFeeds.NightlyDotnet5, "Microsoft.NETCore.App.Ref",        "net5.0-watchos"),
            (WellKnownNuGetFeeds.NightlyXamarin, "Microsoft.watchOS.Ref",            "net5.0-watchos"),
        };

        public static async Task Download(string archivePath)
        {
            var mappingsByTfm = _packageMappings.ToLookup(m => m.Tfm);

            foreach (var tfmGroup in mappingsByTfm)
            {
                var mappings = tfmGroup.ToList();
                var tfm = tfmGroup.Key;

                var folder = Path.Combine(archivePath, tfm);

                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);

                Directory.CreateDirectory(folder);

                foreach (var (feedUrl, id, _) in mappings)
                {
                    var feed = new NuGetFeed(feedUrl);
                    await DownloadPackage(feed, id, folder);
                }
            }
        }

        private static async Task DownloadPackage(NuGetFeed feed, string id, string targetFolder)
        {
            var versions = await feed.GetAllVersionsAsync(id);

            if (!versions.Any())
                return;

            var latest = versions.Max();
            var identity = new PackageIdentity(id, latest);

            System.Console.WriteLine($"Downloading {identity} to {targetFolder}...");

            var package = await feed.GetPackageAsync(identity);

            foreach (var group in package.GetItems("ref"))
            {
                foreach (var item in group.Items)
                {
                    var targetFileName = Path.Combine(targetFolder, Path.GetFileName(item));
                    using var itemStream = package.GetStream(item);
                    using var fileStream = File.Create(targetFileName);
                    await itemStream.CopyToAsync(fileStream);
                }
            }
        }
    }
}
