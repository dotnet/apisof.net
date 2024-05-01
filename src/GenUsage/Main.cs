using System.Diagnostics;

using Terrajobst.ApiCatalog.ActionsRunner;
using Terrajobst.ApiCatalog.Features;

namespace GenUsage;

internal sealed class Main : IConsoleMain
{
    private readonly ApisOfDotNetPathProvider _pathProvider;
    private readonly ApisOfDotNetStore _store;
    private readonly ApisOfDotNetWebHook _webHook;
    private readonly GitHubActionsSummaryTable _summaryTable;

    public Main(ApisOfDotNetPathProvider pathProvider,
                ApisOfDotNetStore store,
                ApisOfDotNetWebHook webHook,
                GitHubActionsSummaryTable summaryTable)
    {
        ThrowIfNull(pathProvider);
        ThrowIfNull(store);
        ThrowIfNull(webHook);
        ThrowIfNull(summaryTable);

        _pathProvider = pathProvider;
        _store = store;
        _webHook = webHook;
        _summaryTable = summaryTable;
    }

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var rootPath = _pathProvider.RootPath;
        var apiUsagesPath = Path.Combine(rootPath, "api-usages");
        var nugetUsagesPath = Path.Combine(apiUsagesPath, "nuget.org.tsv");
        var plannerUsagesPath = Path.Combine(apiUsagesPath, "Upgrade Planner.tsv");
        var netfxCompatLabPath = Path.Combine(apiUsagesPath, "NetFx Compat Lab.tsv");
        var usageDataPath = Path.Combine(rootPath, "usageData.dat");

        var stopwatch = Stopwatch.StartNew();

        await DownloadNuGetUsages(nugetUsagesPath);
        await DownloadPlannerUsages(plannerUsagesPath);
        await DownloadNetFxCompatLabUsages(netfxCompatLabPath);
        await GenerateUsageDataAsync(usageDataPath, apiUsagesPath);
        await UploadUsageData(usageDataPath);

        await _webHook.InvokeAsync(ApisOfDotNetWebHookSubject.UsageData);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private async Task DownloadNuGetUsages(string nugetUsagesPath)
    {
        if (File.Exists(nugetUsagesPath))
        {
            Console.WriteLine($"Skipping download of {Path.GetFileName(nugetUsagesPath)}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(nugetUsagesPath)!);
        await _store.DownloadToAsync("usage", "usages-nuget.tsv", nugetUsagesPath);
    }

    private async Task DownloadPlannerUsages(string plannerUsagesPath)
    {
        if (File.Exists(plannerUsagesPath))
        {
            Console.WriteLine($"Skipping download of {Path.GetFileName(plannerUsagesPath)}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(plannerUsagesPath)!);
        await _store.DownloadToAsync("usage", "usages-planner.tsv", plannerUsagesPath);
    }

    private async Task DownloadNetFxCompatLabUsages(string netfxCompatLabPath)
    {
        if (File.Exists(netfxCompatLabPath))
        {
            Console.WriteLine($"Skipping download of {Path.GetFileName(netfxCompatLabPath)}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(netfxCompatLabPath)!);
        await _store.DownloadToAsync("usage", "usages-netfxcompatlab.tsv", netfxCompatLabPath);
    }

    private Task GenerateUsageDataAsync(string usageDataPath, string apiUsagesPath)
    {
        if (File.Exists(usageDataPath))
        {
            Console.WriteLine($"Skipping generation of {Path.GetFileName(usageDataPath)}");
            return Task.CompletedTask;
        }

        Console.WriteLine($"Generating {Path.GetFileName(usageDataPath)}...");

        var usageFiles = GetUsageFiles(apiUsagesPath);
        var data = new List<(FeatureUsageSource Source, IReadOnlyList<(Guid FeatureId, float Percentage)> Values)>();

        foreach (var (path, name, date) in usageFiles)
        {
            var usageSource = new FeatureUsageSource(name, date);
            var usageSourceData = ParseFile(path).ToArray();
            data.Add((usageSource, usageSourceData));
            AddUsageSourceCountToSummary(usageSource, usageSourceData.Length);
        }

        var usageData = new FeatureUsageData(data);
        usageData.Save(usageDataPath);
        AddFileSizeToSummary(usageDataPath);

        return Task.CompletedTask;

        static IEnumerable<(Guid FeatureId, float Percentage)> ParseFile(string path)
        {
            using var streamReader = new StreamReader(path);

            while (streamReader.ReadLine() is { } line)
            {
                var tabIndex = line.IndexOf('\t');
                var lastTabIndex = line.LastIndexOf('\t');
                if (tabIndex > 0 && tabIndex == lastTabIndex)
                {
                    var guidTextSpan = line.AsSpan(0, tabIndex);
                    var percentageSpan = line.AsSpan(tabIndex + 1);

                    if (Guid.TryParse(guidTextSpan, out var featureId) &&
                        float.TryParse(percentageSpan, out var percentage))
                    {
                        yield return (featureId, percentage);
                    }
                }
            }
        }
    }

    private void AddUsageSourceCountToSummary(FeatureUsageSource usageSource, int usageSourceCount)
    {
        _summaryTable.AppendNumber($"#APIs in {usageSource.Name}", usageSourceCount);
    }

    private void AddFileSizeToSummary(string usageDataPath)
    {
        var fileName = Path.GetFileName(usageDataPath);
        var fileSize = new FileInfo(usageDataPath).Length;
        _summaryTable.AppendBytes(fileName, fileSize);
    }

    private async Task UploadUsageData(string usageDataPath)
    {
        var name = Path.GetFileName(usageDataPath);
        await _store.UploadAsync("usage", name, usageDataPath);
    }

    private static IReadOnlyList<UsageFile> GetUsageFiles(string usagePath)
    {
        var result = new List<UsageFile>();
        var files = Directory.GetFiles(usagePath, "*.tsv");

        foreach (var file in files.OrderBy(f => f))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var date = DateOnly.FromDateTime(File.GetLastWriteTimeUtc(file));
            var usageFile = new UsageFile(file, name, date);
            result.Add(usageFile);
        }

        return result.ToArray();
    }

    internal record UsageFile(string Path, string Name, DateOnly Date);
}