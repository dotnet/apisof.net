using System.Diagnostics;
using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

public static class FrameworkDownloader
{
    public static async Task<int> DownloadAsync(string frameworksPath, string packsPath)
    {
        Validate();

        var entries = new List<FrameworkManifestEntry>();

        var nugetOrg = new NuGetFeed(NuGetFeeds.NuGetOrg);
        var latestNightly = new NuGetFeed(NuGetFeeds.NightlyLatest);

        var releaseFeeds = new[] { nugetOrg };
        var releasePacksPath = Path.Join(packsPath, "release");
        var releaseStore = new NuGetStore(releasePacksPath, releaseFeeds);

        var previewFeeds = new[] { latestNightly, nugetOrg };
        var previewPacksPath = Path.Join(packsPath, "preview");
        var previewStore = new NuGetStore(previewPacksPath, previewFeeds);

        foreach (var framework in FrameworkDefinition.All)
        {
            Console.WriteLine($"Processing packs for {framework.Name}...");

            var store = framework.IsPreview ? previewStore : releaseStore;
            var packs = framework.BuiltInPacks.Concat(framework.WorkloadPacks).ToArray();

            // Let's first determine the set of TFMs for this framework. This
            // will create a set including the base framework plus any platform
            // specific TFMs, such as net5.0-windows.

            var platformFrameworks = new HashSet<NuGetFramework>();

            foreach (var pack in packs)
            {
                var packFrameworks = GetFrameworkNames(framework, pack);
                platformFrameworks.UnionWith(packFrameworks);
            }

            // Now we'll need to restore for each of these TFMs.

            foreach (var platformFramework in platformFrameworks)
            {
                Console.WriteLine($"{platformFramework.GetShortFolderName()}");

                var builder = new PackageGraphBuilder(store, platformFramework);

                foreach (var pack in packs)
                {
                    var packFrameworks = GetFrameworkNames(framework, pack);
                    if (!packFrameworks.Contains(platformFramework))
                        continue;

                    var packVersion = NuGetVersion.Parse(pack.Version);
                    var packIdentity = new PackageIdentity(pack.Name, packVersion);
                    await builder.EnqueueAsync(packIdentity);
                }

                var packageIdentities = await builder.BuildAsync();

                var manifestPackages = new List<FrameworkManifestPackage>();

                foreach (var packageIdentity in packageIdentities)
                {
                    using var package = await store.GetPackageAsync(packageIdentity);

                    var files = new List<string>();

                    var extractedPackageDirectory = Path.Join(store.PackagesCachePath, packageIdentity.Id, packageIdentity.Version.ToNormalizedString());
                    Directory.CreateDirectory(extractedPackageDirectory);

                    var frameworkListPath = package.GetFiles().SingleOrDefault(p => string.Equals(p, "Data/FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
                    var packagePaths = new List<string>();

                    if (frameworkListPath is null)
                    {
                        var group = package.GetCatalogReferenceGroup(platformFramework);
                        if (group is null)
                        {
                            Console.WriteLine($"warning: {packageIdentity}: can't find any ref/lib assets for '{platformFramework}'");
                            continue;
                        }

                        packagePaths.AddRange(group.Items);
                    }
                    else
                    {
                        await using var frameworkListStream = package.GetStream(frameworkListPath);
                        var frameworkList = XDocument.Load(frameworkListStream);

                        foreach (var node in frameworkList.Descendants("File"))
                        {
                            var type = node.Attribute("Type")?.Value;
                            var relativePath = node.Attribute("Path")!.Value;
                            if (type is not null && type != "Managed")
                                continue;

                            relativePath = package.GetFiles().Single(p => p.EndsWith(relativePath, StringComparison.OrdinalIgnoreCase));

                            // TODO: Should we record the profiles?
                            //
                            //       This would be useful for WPF and WinForms in .NET Core 3.x.
                            //
                            // var profileList = node.Attribute("Profile")?.Value ?? string.Empty;
                            // var profiles = profileList.Split(';').Select(p => p.Trim()).ToList();
                            //
                            // if (string.IsNullOrEmpty(platformFramework.Profile) || profiles.Contains(platformFramework.Profile, StringComparer.OrdinalIgnoreCase))
                            //    packagePaths.Add(relativePath);

                            packagePaths.Add(relativePath);
                        }
                    }

                    foreach (var packagePath in packagePaths)
                    {
                        await using var packageStream = package.GetStream(packagePath);
                        var extractedPath = Path.GetFullPath(Path.Join(extractedPackageDirectory, packagePath));
                        var extractedDirectory = Path.GetDirectoryName(extractedPath);
                        Debug.Assert(extractedDirectory is not null);
                        Directory.CreateDirectory(extractedDirectory);
                        await using var extractedStream = File.Create(extractedPath);
                        await packageStream.CopyToAsync(extractedStream);

                        files.Add(extractedPath);
                    }

                    var manifestPackage = new FrameworkManifestPackage(packageIdentity.Id, packageIdentity.Version.ToNormalizedString(), files);
                    manifestPackages.Add(manifestPackage);
                }

                var entry = new FrameworkManifestEntry(platformFramework.GetShortFolderName(), manifestPackages);
                entries.Add(entry);
            }
        }

        Directory.CreateDirectory(frameworksPath);
        var manifestPath = Path.Join(frameworksPath, FrameworkManifest.FileName);
        var manifest = new FrameworkManifest(entries);
        manifest.Save(manifestPath);

        return entries.Count;
    }

    private static IEnumerable<NuGetFramework> GetFrameworkNames(FrameworkDefinition framework, PackReference pack)
    {
        var baseFramework = NuGetFramework.Parse(framework.Name);

        if (pack.Platforms.Count == 0)
        {
            yield return baseFramework;
        }
        else
        {
            foreach (var platform in pack.Platforms)
            {
                if (string.IsNullOrEmpty(platform))
                {
                    yield return baseFramework;
                }
                else
                {
                    var platformFrameworkName = $"{framework.Name}-{platform}";
                    var platformFramework = NuGetFramework.Parse(platformFrameworkName);
                    yield return platformFramework;
                }
            }
        }
    }

    private static void Validate()
    {
        foreach (var framework in FrameworkDefinition.All)
        {
            var nugetFramework = NuGetFramework.Parse(framework.Name);
            var requiresSupportedPlatforms = string.Equals(nugetFramework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
                                             nugetFramework.Version >= new Version(5, 0);

            if (requiresSupportedPlatforms && !framework.SupportedPlatforms.Any())
                Console.WriteLine($"error: '{framework.Name}' doesn't define platforms");

            foreach (var supportedPlatform in framework.SupportedPlatforms)
            {
                if (supportedPlatform.Versions.Count == 0)
                    Console.WriteLine($"error: '{framework.Name}' platform {supportedPlatform.Name} doesn't define any versions");
            }

            if (framework.BuiltInPacks.Count == 0)
                Console.WriteLine($"error: '{framework.Name}' doesn't define built-in packs");

            foreach (var pack in framework.BuiltInPacks)
            {
                if (pack.Kind != PackKind.Framework)
                    Console.WriteLine($"error: '{framework.Name}' built-in pack '{pack.Name}' must be a framework pack");

                if (pack.Workloads.Count > 0)
                    Console.WriteLine($"error: '{framework.Name}' built-in pack '{pack.Name}' can't list workloads");

                ValidatePack(framework, pack);
            }

            foreach (var pack in framework.WorkloadPacks)
            {
                if (pack.Workloads.Count == 0)
                    Console.WriteLine($"error: '{framework.Name}' workload pack '{pack.Name}' must list workloads");

                ValidatePack(framework, pack);
            }

            static void ValidatePack(FrameworkDefinition framework, PackReference pack)
            {
                if (pack.Kind == PackKind.Framework)
                {
                    if (pack.Platforms.Count == 0)
                        Console.WriteLine($"error: '{framework.Name}' framework pack '{pack.Name}' must list platforms");
                }

                if (pack.Kind == PackKind.Library)
                {
                    if (pack.Platforms.Count > 0)
                        Console.WriteLine($"error: '{framework.Name}' library pack '{pack.Name}' can't list platforms");
                }

                foreach (var platform in pack.Platforms)
                    ValidatePlatformIsSupported(framework, pack, platform);
            }

            static void ValidatePlatformIsSupported(FrameworkDefinition framework, PackReference pack, string platform)
            {
                var isNeutral = string.IsNullOrEmpty(platform);
                if (isNeutral)
                    return;

                // Tizen is a special case where we ship some built-in support and rely on others to be on top,
                // so we don't validate that here.
                if (platform == "tizen")
                    return;

                // .NET Core 3.x had built-in support for windows that was modeled differently.
                if (platform == "windows" && (framework.Name == "netcoreapp3.0" || framework.Name == "netcoreapp3.1"))
                    return;

                var nugetFramework = NuGetFramework.Parse($"{framework.Name}-{platform}");
                var platformName = nugetFramework.Platform;
                var platformVersion = nugetFramework.PlatformVersion.GetVersionDisplayString();
                var platformHasVersion = nugetFramework.PlatformVersion != FrameworkConstants.EmptyVersion;

                var supportedPlatform = framework.SupportedPlatforms.SingleOrDefault(p => string.Equals(p.Name, platformName, StringComparison.OrdinalIgnoreCase));
                if (supportedPlatform is null || (platformHasVersion && !supportedPlatform.Versions.Contains(platformVersion)))
                    Console.WriteLine($"error: '{framework.Name}' pack '{pack.Name}' refers to platform '{platform}' which '{framework.Name}' doesn't support");
            }
        }
    }
}