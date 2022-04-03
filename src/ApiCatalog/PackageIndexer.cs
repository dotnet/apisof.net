using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApiCatalog.Frameworks;
using ApiCatalog.Metadata;
using ApiCatalog.NuGet;
using Microsoft.CodeAnalysis;

using NuGet.Frameworks;
using NuGet.Packaging;

namespace ApiCatalog;

public sealed class PackageIndexer
{
    private readonly NuGetStore _store;
    private readonly IReadOnlyList<FrameworkLocator> _frameworkLocators;

    public PackageIndexer(NuGetStore store, IEnumerable<FrameworkLocator> frameworkLocators)
    {
        _store = store;
        _frameworkLocators = frameworkLocators.ToArray();
    }

    public async Task<PackageEntry> Index(string id, string version)
    {
        var dependencies = new Dictionary<string, PackageArchiveReader>();
        var apiIdByGuid = new Dictionary<Guid, int>();
        var frameworkEntries = new List<FrameworkEntry>();
        try
        {
            using (var root = await _store.GetPackageAsync(id, version))
            {
                var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in GetReferenceItems(root))
                    targetNames.Add(item.TargetFramework.GetShortFolderName());

                var targets = targetNames.Select(NuGetFramework.Parse);

                if (!targets.Any())
                    return null;

                foreach (var target in targets)
                {
                    var referenceGroup = GetReferenceItems(root, target);

                    Debug.Assert(referenceGroup != null);

                    await GetDependenciesAsync(dependencies, root, target);

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

                    var platformPaths = GetPlatformSet(target);

                    if (platformPaths == null)
                    {
                        if (!IsKnownUnsupportedPlatform(target))
                            Console.WriteLine($"error: can't resolve platform references for {target}");
                        continue;
                    }

                    foreach (var path in platformPaths)
                    {
                        var metadata = MetadataReference.CreateFromFile(path);
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

    private static bool IsKnownUnsupportedPlatform(NuGetFramework target)
    {
        var platforms = new[] {
            ".NETCore,Version=v5.0",
            ".NETFramework,Version=v4.6.3",
            ".NETPlatform,Version=v5.0",
            ".NETPlatform,Version=v5.4",
            ".NETPortable,Version=v0.0,Profile=aspnetcore50+net45+win8+wp8+wpa81",
            ".NETPortable,Version=v0.0,Profile=net40+sl4+win8",
            ".NETPortable,Version=v0.0,Profile=net40+sl4+win8+wp71+wpa81",
            ".NETPortable,Version=v0.0,Profile=net40+sl4+win8+wp8+wpa81",
            ".NETPortable,Version=v0.0,Profile=net40+win8+wp8+wpa81",
            ".NETPortable,Version=v0.0,Profile=net45+netcore45+wp8+wp81+wpa81",
            ".NETPortable,Version=v0.0,Profile=net45+wp8+wpa81",
            ".NETPortable,Version=v0.0,Profile=net451+win8+wp8+wpa81",
            ".NETPortable,Version=v0.0,Profile=win8+wp8+wpa81",
            ".NETPortable,Version=v0.0,Profile=win8+wpa81",
            "Any,Version=v0.0",
            "ASP.NETCore,Version=v5.0",
            "DNX,Version=v4.5.1",
            "DNXCore,Version=v5.0",
            "native,Version=v0.0",
            "Silverlight,Version=v4.0",
            "Silverlight,Version=v4.0,Profile=WindowsPhone71",
            "Silverlight,Version=v5.0",
            "WindowsPhone,Version=v8.0",
            "WindowsPhoneApp,Version=v8.1",
        };

        var targetString = target.ToString();
        return platforms.Any(p => string.Equals(p, targetString, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<FrameworkSpecificGroup> GetReferenceItems(PackageArchiveReader root)
    {
        // NOTE: We're not using root.GetReferenceItems() because it apparently doesn't always
        //       return items from the ref folder. One package where this reproduces is
        //       System.Security.Cryptography.Csp 4.3.0

        var tfms = new HashSet<string>();

        foreach (var group in root.GetItems("ref").Concat(root.GetItems("lib")))
        {
            if (tfms.Add(group.TargetFramework.GetShortFolderName()))
                yield return group;
        }
    }

    private static FrameworkSpecificGroup GetReferenceItems(PackageArchiveReader root, NuGetFramework current)
    {
        var referenceItems = GetReferenceItems(root);
        var referenceGroup = NuGetFrameworkUtility.GetNearest(referenceItems, current);
        return referenceGroup;
    }

    private async Task GetDependenciesAsync(Dictionary<string, PackageArchiveReader> packages, PackageArchiveReader root, NuGetFramework target)
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

                var dependency = await _store.GetPackageAsync(d.Id, d.VersionRange.MinVersion.ToNormalizedString());
                packages.Add(d.Id, dependency);
                await GetDependenciesAsync(packages, dependency, target);
            }
        }
    }

    private string[] GetPlatformSet(NuGetFramework framework)
    {
        foreach (var l in _frameworkLocators)
        {
            var paths = l.Locate(framework);
            if (paths != null)
                return paths;
        }

        return null;
    }
}