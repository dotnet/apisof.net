namespace Terrajobst.UsageCrawling
{
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

        public string GetGuidsText()
        {
            using var writer = new StringWriter();
            foreach (var key in Data.Keys.OrderBy(k => k))
                writer.WriteLine(key.Guid.ToString("N"));

            return writer.ToString();
        }
    }
}