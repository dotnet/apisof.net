using System.Collections.Concurrent;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace Terrajobst.NetUpgradePlanner;

public sealed class AnalysisReport
{
    public AnalysisReport(ApiCatalogModel catalog,
                          AssemblySet assemblySet,
                          IEnumerable<AnalyzedAssembly> analyzedAssemblies)
    {
        Catalog = catalog;
        AssemblySet = assemblySet;
        AnalyzedAssemblies = analyzedAssemblies.ToArray();
    }

    public ApiCatalogModel Catalog { get; }

    public AssemblySet AssemblySet { get; }

    public IReadOnlyList<AnalyzedAssembly> AnalyzedAssemblies { get; }

    public static AnalysisReport Analyze(ApiCatalogModel catalog,
                                         AssemblySet assemblySet,
                                         AssemblyConfiguration assemblyConfiguration)
    {
        return Analyze(catalog, assemblySet, assemblyConfiguration, IProgressMonitor.Empty);
    }

    public static AnalysisReport Analyze(ApiCatalogModel catalog,
                                         AssemblySet assemblySet,
                                         AssemblyConfiguration assemblyConfiguration,
                                         IProgressMonitor progressMonitor)
    {
        var knownFrameworkAssemblies = catalog.GetAssemblyNames();
        var latestNetFramework = catalog.GetLatestNetFramework();
        var apiByGuid = catalog.AllApis.ToDictionary(a => a.Guid);

        foreach (var api in catalog.AllApis)
        {
            var forwardedApi = catalog.GetForwardedApi(api);
            if (forwardedApi is not null)
                apiByGuid[api.Guid] = forwardedApi.Value;
        }

        var missingFeatureContext = MissingNetFxFeatureContext.Create();

        var desiredFrameworks = assemblySet.Entries.Select(e => assemblyConfiguration.GetDesiredFramework(e))
                                                   .Distinct();
        var platformContextByDesiredFramework = desiredFrameworks.ToDictionary(f => f, f => PlatformAnnotationContext.Create(catalog, f));

        var analyzedAssemblies = new ConcurrentBag<AnalyzedAssembly>();

        var assemblyByName = assemblySet.Entries.ToDictionary(e => e.Name);

        Parallel.ForEach(assemblySet.Entries, entry =>
        {
            progressMonitor.Report(analyzedAssemblies.Count, assemblySet.Entries.Count);

            var assemblyFrameworkName = entry.TargetFramework ?? latestNetFramework;
            var assemblyFramework = NuGetFramework.Parse(assemblyFrameworkName);

            var problems = new List<Problem>();

            // Apis

            var numberOfUsedPlatformApis = 0;
            var numberOfMissingPlatformApis = 0;

            var desiredFrameworkName = assemblyConfiguration.GetDesiredFramework(entry);
            var desiredFramework = NuGetFramework.Parse(desiredFrameworkName);

            var desiredPlatforms = assemblyConfiguration.GetDesiredPlatforms(entry);
            var platformContext = platformContextByDesiredFramework[desiredFrameworkName];

            foreach (var guid in entry.UsedApis)
            {
                if (!apiByGuid.TryGetValue(guid, out var api))
                    continue;

                var netFrameworkAvailability = api.GetAvailability(assemblyFramework);

                // We only care about .NET Framework APIs
                if (netFrameworkAvailability is null)
                    continue;

                // But we don't care about APIs that are merely overrides.
                if (netFrameworkAvailability.Declaration.IsOverride())
                    continue;

                // For the purposes of the score, we only care about framework APIs, not any user
                // APIs or references to packages.
                numberOfUsedPlatformApis++;

                var netCoreAvailability = api.GetAvailability(desiredFramework);
                if (netCoreAvailability is null)
                {
                    var missingFeature = missingFeatureContext.Get(netFrameworkAvailability.Declaration);
                    if (missingFeature is not null)
                    {
                        var text = $"{missingFeature.Name} not available on .NET Core";
                        var url = missingFeature.Url;
                        var details = missingFeature.Description;
                        var category = ProblemCategory.MissingFunctionality;
                        var problemId = new ProblemId(ProblemSeverity.Error, category, text, url);
                        var problem = new Problem(problemId, api, details);
                        problems.Add(problem);
                    }
                    else
                    {
                        var text = "API is missing";
                        var details = "The API isn't available on .NET Core";
                        var url = string.Empty;
                        var category = ProblemCategory.MissingFunctionality;
                        var problemId = new ProblemId(ProblemSeverity.Error, category, text, url);
                        var problem = new Problem(problemId, api, details);
                        problems.Add(problem);
                    }

                    numberOfMissingPlatformApis++;
                }
                else
                {
                    if (netCoreAvailability.Declaration.Obsoletion is not null)
                    {
                        var o = netCoreAvailability.Declaration.Obsoletion.Value;
                        var severity = o.IsError ? ProblemSeverity.Error : ProblemSeverity.Warning;
                        var message = string.IsNullOrEmpty(o.Message)
                            ? "API is obsolete"
                            : o.Message; ;
                        var text = string.IsNullOrEmpty(o.DiagnosticId)
                            ? message
                            : $"{o.DiagnosticId}: {message}";
                        var details = string.Empty;
                        var url = o.Url;
                        var category = ProblemCategory.Obsoletion;
                        var problemId = new ProblemId(severity, category, text, url);
                        var problem = new Problem(problemId, api, details);
                        problems.Add(problem);
                    }

                    var platformAnnotation = platformContext.GetPlatformAnnotation(api);

                    if (platformAnnotation.Kind == PlatformAnnotationKind.None)
                    {
                        // The desired framework has no platform annotations. Ignore.
                    }
                    else if (desiredPlatforms.IsAny && platformAnnotation.Kind != PlatformAnnotationKind.Unrestricted)
                    {
                        var text = "API is not available on all platforms";
                        var details = platformAnnotation.ToString();
                        var url = string.Empty;
                        var category = ProblemCategory.CrossPlatform;
                        var problemId = new ProblemId(ProblemSeverity.Warning, category, text, url);
                        var problem = new Problem(problemId, api, details);
                        problems.Add(problem);
                    }
                    else if (desiredPlatforms.IsSpecific)
                    {
                        var unsupportedPlatforms = desiredPlatforms.Platforms.Where(p => !platformAnnotation.IsSupported(p));
                        foreach (var unsupportedPlatform in unsupportedPlatforms)
                        {
                            var text = $"API is not available on '{unsupportedPlatform}'";
                            var details = platformAnnotation.ToString();
                            var url = string.Empty;
                            var category = ProblemCategory.CrossPlatform;
                            var problemId = new ProblemId(ProblemSeverity.Warning, category, text, url);
                            var problem = new Problem(problemId, api, details);
                            problems.Add(problem);
                        }
                    }
                }
            }

            var numberOfAvailablePlatformApis = numberOfUsedPlatformApis - numberOfMissingPlatformApis;
            var score = (float)numberOfAvailablePlatformApis / numberOfUsedPlatformApis;

            if (float.IsNaN(score))
                score = 1.0f;

            // We don't want portability scores to ever be rounded up to 100%, so let's
            // make sure we round down, keeping one decimal place (which means we need
            // to pass in three because percentage is stored as a float between 0 and 1)

            score = (float)Math.Round(score, 3, MidpointRounding.ToZero);

            // Dependencies

            foreach (var referenceName in entry.Dependencies)
            {
                if (assemblyByName.TryGetValue(referenceName, out var resolvedReference))
                {
                    var referencedFramework = assemblyConfiguration.GetDesiredFramework(resolvedReference);
                    var referencedPlatforms = assemblyConfiguration.GetDesiredPlatforms(resolvedReference);

                    if (!IsCompatible(desiredFrameworkName, referencedFramework))
                    {
                        var severity = ProblemSeverity.Error;
                        var category = ProblemCategory.Consistency;
                        var text = "Incompatible framework in reference";
                        var url = string.Empty;
                        var problemId = new ProblemId(severity, category, text, url);
                        var details = $"Assembly '{entry.Name}' ({desiredFrameworkName}) cannot reference '{referenceName}' ({referencedFramework}) because their frameworks are not compatible.";
                        var problem = new Problem(problemId, referenceName, details);
                        problems.Add(problem);
                    }

                    if (!IsCompatible(desiredPlatforms, referencedPlatforms))
                    {
                        var severity = ProblemSeverity.Warning;
                        var category = ProblemCategory.Consistency;
                        var text = "Incompatible platforms in reference";
                        var url = string.Empty;
                        var problemId = new ProblemId(severity, category, text, url);
                        var details = $"Assembly '{entry.Name}' ({desiredPlatforms.ToDisplayString()}) cannot reference '{referenceName}' ({referencedPlatforms.ToDisplayString()}) because their platforms are not compatible.";
                        var problem = new Problem(problemId, referenceName, details);
                        problems.Add(problem);
                    }
                }
                else if (!knownFrameworkAssemblies.Contains(referenceName))
                {
                    var severity = ProblemSeverity.Warning;
                    var category = ProblemCategory.Consistency;
                    var text = "Unresolved reference";
                    var url = string.Empty;
                    var problemId = new ProblemId(severity, category, text, url);
                    var details = $"Unresolved reference from '{entry.Name}' to '{referenceName}'";
                    var problem = new Problem(problemId, referenceName, details);
                    problems.Add(problem);
                }
            }

            var analyzedAssembly = new AnalyzedAssembly(entry, score, problems);
            analyzedAssemblies.Add(analyzedAssembly);
        });

        var sortedAssemblies = analyzedAssemblies.OrderBy(a => a.Entry.Name);

        return new AnalysisReport(catalog, assemblySet, sortedAssemblies);
    }

    private static bool IsCompatible(string desiredFrameworkName, string referencedFrameworkName)
    {
        var desiredFramework = NuGetFramework.Parse(desiredFrameworkName);
        var referencedFramework = NuGetFramework.Parse(referencedFrameworkName);

        // NOTE: This doesn't do any fallback checking because framework isn't FallbackFramework.
        return NuGetFrameworkUtility.IsCompatibleWithFallbackCheck(desiredFramework, referencedFramework);
    }

    private static bool IsCompatible(PlatformSet desiredPlatforms, PlatformSet referencedPlatforms)
    {
        if (referencedPlatforms.IsAny)
            return true;

        if (desiredPlatforms.IsAny)
            return false;

        foreach (var platform in desiredPlatforms.Platforms)
        {
            if (!referencedPlatforms.Platforms.Contains(platform))
                return false;
        }

        return true;
    }
}
