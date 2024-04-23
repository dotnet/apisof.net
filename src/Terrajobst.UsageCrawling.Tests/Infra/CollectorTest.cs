using Microsoft.Cci;
using Microsoft.CodeAnalysis;

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

        var assembly = new AssemblyBuilder()
            .SetAssembly(source)
            .ToAssembly();

        Check(assembly, expectedUsages);
    }

    protected void Check(string dependencySource, string source, IEnumerable<FeatureUsage> expectedUsages)
    {
        ThrowIfNull(dependencySource);
        ThrowIfNull(source);
        ThrowIfNull(expectedUsages);

        var assembly = new AssemblyBuilder()
            .SetAssembly(source)
            .AddDependency(dependencySource)
            .ToAssembly();

        Check(assembly, expectedUsages);
    }

    protected void Check(IAssembly assembly, IEnumerable<FeatureUsage> expectedUsages)
    {
        Check(assembly, AssemblyContext.Empty, expectedUsages);
    }

    protected void Check(IAssembly assembly, AssemblyContext assemblyContext, IEnumerable<FeatureUsage> expectedUsages)
    {
        var collector = new TCollector();
        collector.Collect(assembly, assemblyContext);

        var expectedFeaturesOrdered = expectedUsages.OrderBy(u => u.FeatureId);
        var actualResultsOrdered = collector.GetResults().Where(Include).OrderBy(u => u.FeatureId);
        Assert.Equal(expectedFeaturesOrdered, actualResultsOrdered);
    }

    protected virtual bool Include(FeatureUsage metric)
    {
        return true;
    }
}
