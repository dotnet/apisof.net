namespace Terrajobst.UsageCrawling;

public sealed class CrawlerResults
{
    public CrawlerResults(IReadOnlyDictionary<ApiKey, int> data)
    {
        Data = data;
    }

    public IReadOnlyDictionary<ApiKey, int> Data { get; }
}