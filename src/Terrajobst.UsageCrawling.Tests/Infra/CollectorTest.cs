using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp;
using Terrajobst.UsageCrawling.Collectors;

namespace Terrajobst.UsageCrawling.Tests.Infra;

public abstract class CollectorTest<TCollector>
    where TCollector: UsageCollector, new()
{
    protected void Check(string source, string lines, Func<string, FeatureUsage> lineToMetricConverter)
    {
        var metrics = new List<FeatureUsage>();

        foreach (var lineSpan in lines.AsSpan().EnumerateLines())
        {
            var line = lineSpan.Trim().ToString();
            var metric = lineToMetricConverter(line);
            metrics.Add(metric);
        }

        Check(source, metrics);
    }

    protected void Check(string source, IEnumerable<FeatureUsage> expectedUsages)
    {
        ThrowIfNull(source);
        ThrowIfNull(expectedUsages);

        var assembly = Compiler.Compile(source, ModifyCompilation);
        Check(assembly, expectedUsages);
    }

    private void Check(IAssembly assembly, IEnumerable<FeatureUsage> expectedUsages)
    {
        var collector = new TCollector();
        collector.Collect(assembly);

        var expectedFeaturesOrdered = expectedUsages.OrderBy(u => u.FeatureId);
        var actualResultsOrdered = collector.GetResults().Where(Include).OrderBy(u => u.FeatureId);
        Assert.Equal(expectedFeaturesOrdered, actualResultsOrdered);
    }

    protected virtual bool Include(FeatureUsage metric)
    {
        return true;
    }

    protected virtual CSharpCompilation ModifyCompilation(CSharpCompilation compilation)
    {
        return compilation;
    }
}