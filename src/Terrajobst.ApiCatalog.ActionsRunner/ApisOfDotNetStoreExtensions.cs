using System.IO.Compression;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public static class ApisOfDotNetStoreExtensions
{
    public static Task DownloadApiCatalogAsync(this ApisOfDotNetStore store, string fileName)
    {
        return store.DownloadToAsync("catalog", "apicatalog.dat", fileName);
    }

    public static async Task UploadApiCatalogAsync(this ApisOfDotNetStore store, string fileName)
    {
        await store.UploadAsync("catalog", "apicatalog.dat", fileName);
    }

    public static async Task UploadSuffixTreeAsync(this ApisOfDotNetStore store, string fileName)
    {
        var compressedFileName = fileName + ".deflate";
        await using (var inputStream = File.OpenRead(fileName))
        await using (var outputStream = File.Create(compressedFileName))
        await using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
            await inputStream.CopyToAsync(deflateStream);

        await store.UploadAsync("catalog", "suffixtree.dat.deflate", compressedFileName);
    }

    public static async Task DownloadNuGetUsageDatabaseAsync(this ApisOfDotNetStore store, string fileName)
    {
        await store.DownloadToAsync("usage", "usages-nuget.db", fileName);
    }

    public static async Task UploadNuGetUsageDatabaseAsync(this ApisOfDotNetStore store, string fileName)
    {
        await store.UploadAsync("usage", "usages-nuget.db", fileName);
    }

    public static async Task UploadNuGetUsageResultsAsync(this ApisOfDotNetStore store, string fileName)
    {
        await store.UploadAsync("usage", "usages-nuget.tsv", fileName);
    }

    public static async Task<(bool, DateTimeOffset?)> DownloadPlannerUsageDatabaseAsync(this ApisOfDotNetStore store, string fileName)
    {
        await store.DownloadToAsync("usage", "usages-planner.db", fileName);
        var timestamp = await store.GetTimestampAsync("usage", "usages-planner.db");
        return (true, timestamp);
    }

    public static async Task UploadPlannerUsageDatabaseAsync(this ApisOfDotNetStore store, string fileName, DateTimeOffset indexTimestamp)
    {
        await store.UploadAsync("usage", "usages-planner.db", fileName);
        await store.SetTimestampAsync("usage", "usages-planner.db", indexTimestamp);
    }

    public static async Task UploadPlannerUsageResultsAsync(this ApisOfDotNetStore store, string fileName)
    {
        await store.UploadAsync("usage", "usages-planner.tsv", fileName);
    }

    public static async Task<IReadOnlyList<string>> GetPlannerFingerprintsAsync(this ApisOfDotNetStore store, DateTimeOffset? since = null)
    {
        return await store.GetBlobNamesAsync("planner", since);
    }

    public static async Task<IReadOnlyList<Guid>> GetPlannerApisAsync(this ApisOfDotNetStore store, string fingerprint)
    {
        var stream = await store.OpenReadAsync("planner", fingerprint);
        var result = new List<Guid>();
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
            if (Guid.TryParse(line, out var guid))
                result.Add(guid);

        return result;
    }

    public static async Task UploadDesignNotesAsync(this ApisOfDotNetStore store, string designNotesPath)
    {
        await store.UploadAsync("catalog", "designNotes.dat", designNotesPath);
    }
}