using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;

using Terrajobst.ApiCatalog;
using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Services;

internal sealed class WorkspaceService
{
    private readonly ProgressService _progressService;
    private readonly CatalogService _catalogService;
    private Workspace _current = Workspace.Default;

    public WorkspaceService(ProgressService progressService,
                            CatalogService catalogService)
    {
        _progressService = progressService;
        _catalogService = catalogService;
    }

    public Workspace Current
    {
        get
        {
            return _current;
        }
        set
        {
            if (_current != value)
            {
                _current = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void Update(Workspace workspace)
    {
        Current = workspace;
    }

    public void Clear()
    {
        Update(Workspace.Default);
    }

    public async Task AddAssembliesAsync(IEnumerable<string> paths)
    {
        var catalog = await _catalogService.GetAsync();

        var pathSet = await _progressService.Run(pm => AssemblySet.Create(paths, pm), "Reading assemblies...");

        // Let's remove known framework assemblies and resource files.
        var frameworkAssemblyNames = catalog.GetAssemblyNames();
        var frameworkAssemblies = pathSet.Entries.Where(e => frameworkAssemblyNames.Contains(e.Name) ||
                                                             e.Name.EndsWith(".resources"))
                                                 .ToArray();
        pathSet = pathSet.Remove(frameworkAssemblies);

        var oldWorkspace = _current;
        var oldAssemblySet = oldWorkspace.AssemblySet;
        var newAssemblySet = oldAssemblySet.Merge(pathSet);
        var newAssemblies = newAssemblySet.Entries.Where(e => !oldAssemblySet.Entries.Contains(e));
        var oldConfiguration = oldWorkspace.AssemblyConfiguration;
        var newConfiguration = AddAssemblyConfiguration(catalog, newAssemblySet, newAssemblies, oldConfiguration);

        var newWorkspace = new Workspace(newAssemblySet, newConfiguration, null);
        Update(newWorkspace);

        await AnalyzeAsync();
    }

    public async Task RemoveAssembliesAsync(IEnumerable<AssemblySetEntry> assemblies)
    {
        var oldWorkspace = _current;
        var oldAssemblySet = oldWorkspace.AssemblySet;
        var newAssemblySet = oldWorkspace.AssemblySet.Remove(assemblies);
        var removedAssemblies = oldAssemblySet.Entries.Where(e => !newAssemblySet.Entries.Contains(e));

        var oldConfiguration = oldWorkspace.AssemblyConfiguration;
        var newConfiguration = RemoveAssemblyConfiguration(removedAssemblies, oldConfiguration);

        var newWorkspace = new Workspace(newAssemblySet, newConfiguration, null);
        Update(newWorkspace);

        // NOTE: We used to be smart here where we'd just remove the analysis results for the
        //       assemblies being removed. This no longer works as the analysis also contains
        //       problems for dependencies (like missing dependencies, incompatible frameworks
        //       etc). We could try to handle them all here but that feels brittle, compared
        //       to just re-analyzing the world...

        await AnalyzeAsync();
    }

    public async Task AnalyzeAsync()
    {
        var catalog = await _catalogService.GetAsync();
        var oldWorkspace = _current;
        var assemblySet = oldWorkspace.AssemblySet;
        var assemblyConfiguration = oldWorkspace.AssemblyConfiguration;

        var report = await _progressService.Run(pm => AnalysisReport.Analyze(catalog, assemblySet, assemblyConfiguration, pm), "Analyzing...");
        var newWorkspace = new Workspace(assemblySet, assemblyConfiguration, report);
        Update(newWorkspace);
    }

    private static AssemblyConfiguration AddAssemblyConfiguration(ApiCatalogModel catalog,
                                                                  AssemblySet assemblySet,
                                                                  IEnumerable<AssemblySetEntry> newAssemblies,
                                                                  AssemblyConfiguration oldConfiguration)
    {
        var inference = new FrameworkAndPlatformInference(catalog, assemblySet, newAssemblies, oldConfiguration);
        var result = oldConfiguration;

        foreach (var assembly in newAssemblies)
        {
            var desiredFramework = inference.InferDesiredFramework(assembly);
            var desiredPlatforms = inference.InferDesiredPlatforms(desiredFramework);

            result = result.SetDesiredFramework(assembly, desiredFramework)
                           .SetDesiredPlatforms(assembly, desiredPlatforms);
        }

        return result;
    }

    private static AssemblyConfiguration RemoveAssemblyConfiguration(IEnumerable<AssemblySetEntry> removedAssemblies,
                                                                     AssemblyConfiguration oldConfiguration)
    {
        return oldConfiguration.RemoveRange(removedAssemblies);
    }

    public async Task SetDesiredFrameworkAsync(IEnumerable<AssemblySetEntry> selectedEntries, string framework)
    {
        var newConfiguration = _current.AssemblyConfiguration;

        var parsedFramework = NuGetFramework.Parse(framework);
        var forcedPlatform = parsedFramework.HasPlatform
                              ? PlatformSet.For(new[] { parsedFramework.Platform })
                              : (PlatformSet?)null;

        foreach (var assembly in selectedEntries)
        {
            newConfiguration = newConfiguration.SetDesiredFramework(assembly, framework);

            if (forcedPlatform is not null)
                newConfiguration = newConfiguration.SetDesiredPlatforms(assembly, forcedPlatform.Value);
        }

        UpdateAssemblyConfiguration(newConfiguration);

        await AnalyzeAsync();
    }

    public async Task SetDesiredPlatformsAsync(IEnumerable<AssemblySetEntry> selectedEntries, PlatformSet platforms)
    {
        var newConfiguration = _current.AssemblyConfiguration;

        foreach (var assembly in selectedEntries)
            newConfiguration = newConfiguration.SetDesiredPlatforms(assembly, platforms);

        UpdateAssemblyConfiguration(newConfiguration);

        await AnalyzeAsync();
    }

    private void UpdateAssemblyConfiguration(AssemblyConfiguration newConfiguration)
    {
        if (newConfiguration == _current.AssemblyConfiguration)
            return;

        var assemblySet = _current.AssemblySet;
        var report = _current.Report;
        var workspace = new Workspace(assemblySet, newConfiguration, report);
        Update(workspace);
    }

    public event EventHandler? Changed;
}
