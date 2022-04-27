using System.Collections.Concurrent;

using Microsoft.Cci.Extensions;

using NuGet.Frameworks;

using Terrajobst.ApiCatalog;
using Terrajobst.UsageCrawling;

internal static class ApiAvailabilityChecker
{
    public static void Run(ApiCatalogModel catalog,
                           IReadOnlyCollection<string> filePaths,
                           IReadOnlyList<NuGetFramework> frameworks,
                           Action<AssemblyAvailabilityResult> resultReceiver)
    {
        var apiByGuid = catalog.GetAllApis().ToDictionary(a => a.Guid);
        var availabilityContext = ApiAvailabilityContext.Create(catalog);

        foreach (var api in catalog.GetAllApis())
        {
            var forwardedApi = catalog.GetForwardedApi(api);
            if (forwardedApi is not null)
                apiByGuid[api.Guid] = forwardedApi.Value;
        }

        var apiAvailability = new ConcurrentDictionary<ApiModel, ApiAvailability>();

        var resultSink = new BlockingCollection<AssemblyAvailabilityResult>();

        var resultSinkTask = Task.Run(() =>
        {
            foreach (var result in resultSink.GetConsumingEnumerable())
                resultReceiver(result);
        });

        Parallel.ForEach(filePaths, filePath =>
        {
            using var env = new HostEnvironment();
            var assembly = env.LoadAssemblyFrom(filePath);
            var assemblyName = assembly is not null
                                ? assembly.Name.Value
                                : Path.GetFileName(filePath);
            if (assembly is null)
            {
                var result = new AssemblyAvailabilityResult(assemblyName, "Not a valid .NET assembly", Array.Empty<ApiAvailabilityResult>());
                resultSink.Add(result);
            }
            else
            {
                var crawler = new AssemblyCrawler();
                crawler.Crawl(assembly);

                var crawlerResults = crawler.GetResults();

                var apiResults = new List<ApiAvailabilityResult>();
                var frameworkResultBuilder = new List<AvailabilityResult>(frameworks.Count);

                foreach (var apiKey in crawlerResults.Data.Keys)
                {
                    if (apiByGuid.TryGetValue(apiKey.Guid, out var api))
                    {
                        var availability = apiAvailability.GetOrAdd(api, a => availabilityContext.GetAvailability(a));

                        frameworkResultBuilder.Clear();

                        foreach (var framework in frameworks)
                        {
                            var infos = availability.Frameworks.Where(fx => fx.Framework == framework).ToArray();

                            // NOTE: There are APIs that exist in multiple places in-box, e.g. Microsoft.Windows.Themes.ListBoxChrome.
                            //       It doesn't really matter for our purposes. Either way, we'll pick the first one.
                            var info = infos.FirstOrDefault(i => i.IsInBox) ?? infos.FirstOrDefault(i => !i.IsInBox);

                            if (info is null)
                            {
                                frameworkResultBuilder.Add(AvailabilityResult.Unavailable);
                            }
                            else if (info.IsInBox)
                            {
                                frameworkResultBuilder.Add(AvailabilityResult.AvailableInBox);
                            }
                            else
                            {
                                frameworkResultBuilder.Add(AvailabilityResult.AvailableInPackage(info.Package.Value));
                            }
                        }

                        var frameworkResults = frameworkResultBuilder.ToArray();
                        var apiResult = new ApiAvailabilityResult(api, frameworkResults);
                        apiResults.Add(apiResult);
                    }
                }

                var results = new AssemblyAvailabilityResult(assemblyName, null, apiResults.ToArray());
                resultSink.Add(results);
            }
        });

        resultSink.CompleteAdding();
        resultSinkTask.Wait();
    }
}