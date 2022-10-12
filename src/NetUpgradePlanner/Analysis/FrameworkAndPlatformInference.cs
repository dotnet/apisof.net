using System;
using System.Collections.Generic;
using System.Linq;

using NetUpgradePlanner.Analysis;

using NuGet.Frameworks;

using Terrajobst.ApiCatalog;

namespace NetUpgradePlanner.Services;

internal sealed class FrameworkAndPlatformInference
{
    private readonly Dictionary<string, AssemblySetEntry> _assemblyByName;
    private readonly Dictionary<AssemblySetEntry, string> _inferredFramework = new();
    private readonly Dictionary<AssemblySetEntry, PlatformSet> _inferredPlatforms = new();
    private readonly HashSet<Guid> _systemWindowsApis;
    private readonly string _latestNetCore;
    private readonly string _latestNetCoreWindows;

    public FrameworkAndPlatformInference(ApiCatalogModel catalog,
                                         AssemblySet assemblySet,
                                         IEnumerable<AssemblySetEntry> newAssemblies,
                                         AssemblyConfiguration configuration)
    {
        _assemblyByName = assemblySet.Entries.ToDictionary(e => e.Name);
        _systemWindowsApis = catalog.RootApis.Where(r => r.Name == "System.Windows" ||
                                                         r.Name.StartsWith("System.Windows."))
                                             .SelectMany(a => a.DescendantsAndSelf())
                                             .Select(a => a.Guid)
                                             .ToHashSet();

        _latestNetCore = GetLatestFramework(catalog, ".NETCoreApp");
        _latestNetCoreWindows = GetLatestFramework(catalog, ".NETCoreApp", "windows");

        var newAssembliesSet = newAssemblies.ToHashSet();

        foreach (var assembly in assemblySet.Entries)
        {
            if (!newAssembliesSet.Contains(assembly))
            {
                var inferredFramework = configuration.GetDesiredFramework(assembly);
                var inferredPlatforms = configuration.GetDesiredPlatforms(assembly);

                _inferredFramework[assembly] = inferredFramework;
                _inferredPlatforms[assembly] = inferredPlatforms;
            }
        }

        foreach (var assembly in newAssemblies)
            SetInferredFramework(assembly);
    }

    private IEnumerable<AssemblySetEntry> GetDependencies(AssemblySetEntry assembly)
    {
        return assembly.Dependencies.Where(n => _assemblyByName.ContainsKey(n))
                                    .Select(n => _assemblyByName[n]);
    }

    private void SetInferredFramework(AssemblySetEntry assembly)
    {
        if (_inferredFramework.ContainsKey(assembly))
            return;

        foreach (var dependency in GetDependencies(assembly))
            SetInferredFramework(dependency);

        _inferredFramework[assembly] = InferFramework(assembly);
    }

    public string InferFramework(AssemblySetEntry assembly)
    {
        var result = DependsOnSystemWindows(assembly)
                      ? _latestNetCoreWindows
                      : _latestNetCore;

        if (assembly.TargetFramework is not null && IsCompatible(assembly.TargetFramework, result))
            return assembly.TargetFramework;

        return result;
    }

    public string InferDesiredFramework(AssemblySetEntry assembly)
    {
        return _inferredFramework[assembly];
    }

    public PlatformSet InferDesiredPlatforms(string framework)
    {
        var parsed = NuGetFramework.Parse(framework);
        if (parsed.HasPlatform)
            return PlatformSet.For(new[] { parsed.Platform });

        return PlatformSet.Any;
    }

    private bool DependsOnSystemWindows(AssemblySetEntry assembly)
    {
        foreach (var dependency in GetDependencies(assembly))
        {
            if (_inferredFramework.TryGetValue(dependency, out var inferredFramework))
            {
                var dependencyPlatform = NuGetFramework.Parse(inferredFramework).Platform;
                var isDependencyTargetingWindows = string.Equals(dependencyPlatform, "Windows", StringComparison.OrdinalIgnoreCase);
                if (isDependencyTargetingWindows)
                    return true;
            }
        }

        foreach (var api in assembly.UsedApis)
        {
            if (_systemWindowsApis.Contains(api))
                return true;
        }

        return false;
    }

    private static string GetLatestFramework(ApiCatalogModel catalog, string frameworkIdentifier, string? platformSuffix = null)
    {
        var framework = catalog.Frameworks
                               .Select(f => NuGetFramework.Parse(f.Name))
                               .Where(f => string.Equals(f.Framework, frameworkIdentifier, StringComparison.OrdinalIgnoreCase))
                               .Where(f => !f.HasPlatform && platformSuffix is null ||
                                           f.HasPlatform && platformSuffix is not null && string.Equals(f.Platform, platformSuffix, StringComparison.OrdinalIgnoreCase))
                               .MaxBy(f => f.Version)!;

        return framework.GetShortFolderName();
    }

    private static bool IsCompatible(string currentFramework, string desiredFramework)
    {
        var assemblyFramework = NuGetFramework.Parse(currentFramework);
        var framework = NuGetFramework.Parse(desiredFramework);

        // NOTE: This doesn't do any fallback checking because framework isn't FallbackFramework.
        return NuGetFrameworkUtility.IsCompatibleWithFallbackCheck(framework, assemblyFramework);
    }
}
