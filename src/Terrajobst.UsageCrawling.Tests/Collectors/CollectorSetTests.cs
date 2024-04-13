using Terrajobst.UsageCrawling.Collectors;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class CollectorSetTests
{
    [Fact]
    public void CollectorSet_Current_IsMaxOfIntroducedIn()
    {
        var collectorSet = new UsageCollectorSet();
        var expectedVersion = collectorSet.Collectors.Max(c => c.VersionIntroduced);
        var actualVersion = UsageCollectorSet.CurrentVersion;

        Assert.Equal(expectedVersion, actualVersion);
    }

    [Fact]
    public void CollectorSet_Collectors_ExposesAllCollectors()
    {
        var collectorSet = new UsageCollectorSet();
        var expectedTypes = typeof(UsageCollectorSet).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(UsageCollector).IsAssignableFrom(t))
            .OrderBy(t => t.FullName);
        var actualTypes = collectorSet.Collectors.Select(c => c.GetType()).OrderBy(t => t.FullName);

        Assert.Equal(expectedTypes, actualTypes);
    }
}