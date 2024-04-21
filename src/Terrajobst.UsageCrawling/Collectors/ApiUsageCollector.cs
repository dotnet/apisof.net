using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class ApiUsageCollector : UsageCollector
{
    private readonly AssemblyCrawler _crawler = new();

    public override int VersionRequired => 1;

    public override void Collect(IAssembly assembly)
    {
        ThrowIfNull(assembly);

        _crawler.Crawl(assembly);
    }

    public override IEnumerable<FeatureUsage> GetResults()
    {
        var crawlerResults = _crawler.GetResults();
        var result = new FeatureUsage[crawlerResults.Data.Count];
        var index = 0;
        foreach (var key in crawlerResults.Data.Keys)
        {
            result[index] = FeatureUsage.ForApi(key);
            index++;
        }

        return result;
    }
}