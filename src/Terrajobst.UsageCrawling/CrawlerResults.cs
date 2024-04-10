namespace Terrajobst.UsageCrawling;

public sealed class CrawlerResults
{
    public CrawlerResults(IReadOnlyDictionary<ApiKey, int> data)
    {
        Data = data;
    }

    public IReadOnlyDictionary<ApiKey, int> Data { get; }

    public async Task WriteGuidsAsync(string fileName)
    {
        await using var writer = File.CreateText(fileName);
        await WriteGuidsAsync(writer);
    }

    public async Task WriteGuidsAsync(TextWriter writer)
    {
        foreach (var key in Data.Keys.OrderBy(k => k))
            await writer.WriteLineAsync(key.Guid.ToString("N"));
    }
}