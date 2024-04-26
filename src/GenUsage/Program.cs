using System.Diagnostics;
using System.Text.Json;

using Azure.Core;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration.UserSecrets;

using Terrajobst.ApiCatalog.Features;

namespace GenUsage;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length > 1)
        {
            var exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            Console.Error.Write("error: incorrect number of arguments");
            Console.Error.Write($"usage: {exeName} [<download-directory>]");
            return -1;
        }

        var environmentPath = Environment.GetEnvironmentVariable("APISOFDOTNET_INDEX_PATH");
        var defaultPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Catalog");
        var rootPath = args.Length == 1
            ? args[0]
            : environmentPath ?? defaultPath;

        var success = true;

        try
        {
            await RunAsync(rootPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        if (success)
            await PostToGenCatalogWebHook();

        return success ? 0 : -1;
    }
    
    private static async Task RunAsync(string rootPath)
    {
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

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }
    
    private static async Task DownloadNuGetUsages(string nugetUsagesPath)
    {
        if (File.Exists(nugetUsagesPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(nugetUsagesPath)!);

        Console.WriteLine("Downloading NuGet usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-nuget.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(nugetUsagesPath);
        File.SetLastWriteTimeUtc(nugetUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadPlannerUsages(string plannerUsagesPath)
    {
        if (File.Exists(plannerUsagesPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(plannerUsagesPath)!);

        Console.WriteLine("Downloading Planner usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-planner.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(plannerUsagesPath);
        File.SetLastWriteTimeUtc(plannerUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadNetFxCompatLabUsages(string netfxCompatLabPath)
    {
        if (File.Exists(netfxCompatLabPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(netfxCompatLabPath)!);

        Console.WriteLine("Downloading NetFx Compat Lab usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-netfxcompatlab.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(netfxCompatLabPath);
        File.SetLastWriteTimeUtc(netfxCompatLabPath, lastModified.UtcDateTime);
    }
    
    private static Task GenerateUsageDataAsync(string usageDataPath, string apiUsagesPath)
    {
        if (File.Exists(usageDataPath))
            return Task.CompletedTask;

        Console.WriteLine($"Generating {Path.GetFileName(usageDataPath)}...");

        var usageFiles = GetUsageFiles(apiUsagesPath);
        var data = new List<(FeatureUsageSource Source, IReadOnlyList<(Guid FeatureId, float Percentage)> Values)>();

        foreach (var (path, name, date) in usageFiles)
        {
            var usageSource = new FeatureUsageSource(name, date);
            var usageSourceData = ParseFile(path).ToArray();
            data.Add((usageSource, usageSourceData));
        }

        var usageData = new FeatureUsageData(data);
        usageData.Save(usageDataPath);

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
    
    private static async Task UploadUsageData(string usageDataPath)
    {
        Console.WriteLine("Uploading usage data...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "usage";
        var name = Path.GetFileName(usageDataPath);
        var blobClient = new BlobClient(connectionString, container, name, options: GetBlobOptions());
        await blobClient.UploadAsync(usageDataPath, overwrite: true);
    }

    private static async Task PostToGenCatalogWebHook()
    {
        Console.WriteLine("Invoking webhook...");
        var secrets = Secrets.Load();

        var url = Environment.GetEnvironmentVariable("GenCatalogWebHookUrl");
        if (string.IsNullOrEmpty(url))
            url = secrets?.GenCatalogWebHookUrl;

        var secret = Environment.GetEnvironmentVariable("GenCatalogWebHookSecret");
        if (string.IsNullOrEmpty(secret))
            secret = secrets?.GenCatalogWebHookSecret;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(secret))
        {
            Console.WriteLine("warning: cannot retrieve secret for GenCatalog web hook.");
            return;
        }

        try
        {
            var client = new HttpClient();
            var response = await client.PostAsync(url, new StringContent(secret));
            Console.WriteLine($"Webhook returned: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"warning: there was a problem calling the web hook: {ex}");
        }
    }
    
    private static BlobClientOptions GetBlobOptions()
    {
        return new BlobClientOptions
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(90),
                MaxRetries = 10,
                NetworkTimeout = TimeSpan.FromMinutes(5),
            }
        };
    }
    
    private static string GetAzureStorageConnectionString()
    {
        var result = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
        if (string.IsNullOrEmpty(result))
        {
            var secrets = Secrets.Load();
            result = secrets?.AzureStorageConnectionString;
        }

        if (string.IsNullOrEmpty(result))
            throw new Exception("Cannot retrieve connection string for Azure blob storage. You either need to define an environment variable or a user secret.");

        return result;
    }
    
    internal sealed class Secrets
    {
        public string? AzureStorageConnectionString { get; set; }
        public string? GenCatalogWebHookUrl { get; set; }
        public string? GenCatalogWebHookSecret { get; set; }

        public static Secrets? Load()
        {
            var secretsPath = PathHelper.GetSecretsPathFromSecretsId("ApiCatalog");
            if (!File.Exists(secretsPath))
                return null;

            var secretsJson = File.ReadAllText(secretsPath);
            return JsonSerializer.Deserialize<Secrets>(secretsJson)!;
        }
    }
    
    internal record UsageFile(string Path, string Name, DateOnly Date);
}