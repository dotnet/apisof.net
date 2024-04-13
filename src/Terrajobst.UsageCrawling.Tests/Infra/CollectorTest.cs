using Microsoft.Cci;
using Terrajobst.UsageCrawling.Collectors;

namespace Terrajobst.UsageCrawling.Tests.Infra;

public abstract class CollectorTest<TCollector>
    where TCollector: UsageCollector, new()
{
    protected void Check(string source, string lines, Func<string, UsageMetric> lineToMetricConverter)
    {
        var metrics = new List<UsageMetric>();

        foreach (var lineSpan in lines.AsSpan().EnumerateLines())
        {
            var line = lineSpan.Trim().ToString();
            var metric = lineToMetricConverter(line);
            metrics.Add(metric);
        }

        Check(source, metrics);
    }

    protected void Check(string source, IEnumerable<UsageMetric> expectedMetrics)
    {
        ThrowIfNull(source);
        ThrowIfNull(expectedMetrics);

        var assembly = Compiler.Compile(source);
        Check(assembly, expectedMetrics);
    }

    private void Check(IAssembly assembly, IEnumerable<UsageMetric> expectedMetrics)
    {
        var collector = new TCollector();
        collector.Collect(assembly);

        var expectedFeaturesOrdered = expectedMetrics.OrderBy(m => m.Guid);
        var actualResultsOrdered = collector.GetResults().Where(Include).OrderBy(m => m.Guid);
        Assert.Equal(expectedFeaturesOrdered, actualResultsOrdered);
    }

    protected virtual bool Include(UsageMetric metric)
    {
        return true;
    }
}