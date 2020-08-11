using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using NuGet.Frameworks;
using NuGet.Packaging;

namespace ApiCatalog
{
    public static class PackageIndexer
    {
        private static readonly string _v3_flatContainer_nupkg_template = "https://api.nuget.org/v3-flatcontainer/{0}/{1}/{0}.{1}.nupkg";
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<PackageEntry> Index(string id, string version)
        {
            var dependencies = new Dictionary<string, PackageArchiveReader>();
            var apiIdByGuid = new Dictionary<Guid, int>();
            var frameworkEntries = new List<FrameworkEntry>();
            try
            {
                using (var root = await FetchPackageAsync(id, version))
                {
                    var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var item in root.GetReferenceItems().Concat(root.GetLibItems()))
                        targetNames.Add(item.TargetFramework.GetShortFolderName());

                    var targets = targetNames.Select(NuGetFramework.Parse);

                    if (!targets.Any())
                        return null;

                    foreach (var target in targets)
                    {
                        var referenceGroup = GetReferenceItems(root, target);

                        Debug.Assert(referenceGroup != null);

                        await FetchDependenciesAsync(dependencies, root, target);

                        // Add references

                        var referenceMetadata = new List<MetadataReference>();

                        foreach (var path in referenceGroup.Items)
                        {
                            var metadata = await AssemblyStream.CreateAsync(root.GetStream(path), path);
                            referenceMetadata.Add(metadata);
                        }

                        // Add dependencies

                        var dependencyMetadata = new List<MetadataReference>();

                        foreach (var dependency in dependencies.Values)
                        {
                            var dependencyReferences = GetReferenceItems(dependency, target);
                            if (dependencyReferences != null)
                            {
                                foreach (var path in dependencyReferences.Items)
                                {
                                    var metadata = await AssemblyStream.CreateAsync(dependency.GetStream(path), path);
                                    dependencyMetadata.Add(metadata);
                                }
                            }
                        }

                        // Add framework

                        var plaformSet = await GetPlatformSet(target);

                        if (plaformSet == null)
                        {
                            Console.WriteLine($"error: can't resolve platform references for {target}");
                            continue;
                        }

                        foreach (var (path, stream) in plaformSet.GetFiles())
                        {
                            var metadata = await AssemblyStream.CreateAsync(stream, path);
                            dependencyMetadata.Add(metadata);
                        }

                        var metadataContext = MetadataContext.Create(referenceMetadata, dependencyMetadata);

                        var assemblyEntries = new List<AssemblyEntry>();

                        foreach (var reference in metadataContext.Assemblies)
                        {
                            var entry = AssemblyEntry.Create(reference);
                            assemblyEntries.Add(entry);
                        }

                        frameworkEntries.Add(FrameworkEntry.Create(target.GetShortFolderName(), assemblyEntries));
                    }
                }

                return PackageEntry.Create(id, version, frameworkEntries);
            }
            finally
            {
                foreach (var package in dependencies.Values)
                    package.Dispose();
            }
        }

        private static FrameworkSpecificGroup GetReferenceItems(PackageArchiveReader root, NuGetFramework current)
        {
            var referenceItems = root.GetReferenceItems();
            var referenceGroup = NuGetFrameworkUtility.GetNearest(referenceItems, current);
            return referenceGroup;
        }

        private static async Task FetchDependenciesAsync(Dictionary<string, PackageArchiveReader> packages, PackageArchiveReader root, NuGetFramework target)
        {
            var dependencies = root.GetPackageDependencies();
            var dependencyGroup = NuGetFrameworkUtility.GetNearest(dependencies, target);
            if (dependencyGroup != null)
            {
                foreach (var d in dependencyGroup.Packages)
                {
                    if (packages.TryGetValue(d.Id, out var existingPackage))
                    {
                        if (d.VersionRange.MinVersion > existingPackage.NuspecReader.GetVersion())
                        {
                            existingPackage.Dispose();
                            packages.Remove(d.Id);
                            existingPackage = null;
                        }
                    }

                    if (existingPackage != null)
                        continue;

                    Console.WriteLine($"Discovered dependency {d}");
                    var dependency = await FetchPackageAsync(d.Id, d.VersionRange.MinVersion.ToNormalizedString());
                    packages.Add(d.Id, dependency);
                    await FetchDependenciesAsync(packages, dependency, target);
                }
            }
        }

        private static async Task<PackageArchiveReader> FetchPackageAsync(string id, string version)
        {
            var url = GetFlatContainerNupkgUrl(id, version);
            var nupkgStream = await _httpClient.GetStreamAsync(url);
            return new PackageArchiveReader(nupkgStream);
        }

        private static Uri GetFlatContainerNupkgUrl(string id, string version)
        {
            var url = string.Format(_v3_flatContainer_nupkg_template, id, version);
            return new Uri(url);
        }

        private static Task<FileSet> GetPlatformSet(NuGetFramework framework)
        {
            if (framework.Framework == ".NETCoreApp")
                return GetNetCore();
            else if (framework.Framework == ".NETStandard")
                return GetNetStandard();
            else if (framework.Framework == ".NETFramework")
                return GetNetFramework();
            else if (framework.Framework == ".NETPortable")
                return Task.FromResult(GetPortableFramework(framework.Profile));
            else
                return Task.FromResult<FileSet>(null);
        }

        private static async Task<FileSet> GetNetCore()
        {
            return new PlatformPackageSet
            {
                Packages = new[]
                {
                    await FetchPackageAsync("Microsoft.AspNetCore.App.Ref", "3.1.0"),
                    await FetchPackageAsync("Microsoft.NETCore.App.Ref", "3.1.0"),
                    await FetchPackageAsync("Microsoft.WindowsDesktop.App.Ref", "3.1.0")
                },
                Selector = pr => pr.GetFiles()
                                   .Where(path => path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase) &&
                                                  path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (p, pr.GetStream(p)))
            };
        }

        private static async Task<FileSet> GetNetStandard()
        {
            return new PlatformPackageSet
            {
                Packages = new[]
                {
                    await FetchPackageAsync("NETStandard.Library.Ref", "2.1.0")
                },
                Selector = pr => pr.GetFiles()
                                   .Where(path => path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase) &&
                                                  path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (p, pr.GetStream(p)))
            };
        }

        private static async Task<FileSet> GetNetFramework()
        {
            return new PlatformPackageSet
            {
                Packages = new[]
                {
                    await FetchPackageAsync("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.0")
                },
                Selector = pr => pr.GetFiles()
                                   .Where(path => path.StartsWith("build/.NETFramework/", StringComparison.OrdinalIgnoreCase) &&
                                                  path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (p, pr.GetStream(p)))
            };
        }

        private static FileSet GetPortableFramework(string profile)
        {
            var root = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETPortable";
            var versionDirectories = Directory.GetDirectories(root);

            foreach (var versionDirectory in versionDirectories)
            {
                var profileDirectory = Path.Join(versionDirectory, "Profile", profile);
                if (Directory.Exists(profileDirectory))
                {
                    var paths = Directory.GetFiles(versionDirectory, "*.dll");
                    return new PathFileSet(paths);
                }
            }

            return null;
        }
    }
}
