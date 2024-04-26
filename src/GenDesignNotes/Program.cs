using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Azure.Core;
using Azure.Storage.Blobs;

using LibGit2Sharp;
using Microsoft.Extensions.Configuration.UserSecrets;

using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Generation.DesignNotes;

namespace GenDesignNotes;

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
        var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");
        var reviewRepoPath = Path.Combine(rootPath, "apireviews");
        var designNotesPath = Path.Combine(rootPath, "designNotes.dat");

        var stopwatch = Stopwatch.StartNew();

        await DownloadCatalog(catalogModelPath);
        await DownloadApiReviewsRepoAsync(reviewRepoPath);
        await GenerateDesignNotesAsync(reviewRepoPath, catalogModelPath, designNotesPath);
        await UploadDesignNotesAsync(designNotesPath);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private static async Task DownloadCatalog(string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(catalogModelPath)!);

        Console.WriteLine("Downloading API Catalog...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "catalog", "apicatalog.dat", options: GetBlobOptions());
        await blobClient.DownloadToAsync(catalogModelPath);
    }
    
    private static Task DownloadApiReviewsRepoAsync(string reviewRepoPath)
    {
        if (Directory.Exists(reviewRepoPath))
            return Task.CompletedTask;

        var url = "https://github.com/dotnet/apireviews";
        Console.WriteLine($"Cloning {url}...");
        Repository.Clone(url, reviewRepoPath);
        return Task.CompletedTask;
    }

    private static async Task GenerateDesignNotesAsync(string reviewRepoPath, string catalogModelPath, string designNotesPath)
    {
        if (File.Exists(designNotesPath))
            return;

        Console.WriteLine("Generating design notes...");
        var reviewDatabase = ReviewDatabase.Load(reviewRepoPath);
        var catalog = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var linkDatabase = DesignNoteBuilder.Build(reviewDatabase, catalog);
        linkDatabase.Save(designNotesPath);
    }
    
    private static async Task UploadDesignNotesAsync(string designNotesPath)
    {
        Console.WriteLine("Uploading design notes...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "designNotes.dat", options: GetBlobOptions());
        await blobClient.UploadAsync(designNotesPath, overwrite: true);
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
}