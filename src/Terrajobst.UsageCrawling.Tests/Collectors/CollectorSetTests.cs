using Terrajobst.UsageCrawling.Collectors;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class CollectorSetTests
{
    [Fact]
    public void CollectorSet_Current_IsMaxOfVersionRequired()
    {
        var collectorSet = new UsageCollectorSet();
        var highestCollectorVersion = collectorSet.Collectors.Max(c => c.VersionRequired);
        var currentVersion = UsageCollectorSet.CurrentVersion;

        Assert.True(highestCollectorVersion == currentVersion,
                    $"UsageCollectorSet.CurrentVersion is {currentVersion} but should be the highest collector version, which is {highestCollectorVersion}.");
    }

    [Fact]
    public void CollectorSet_NoCollector_ExceedsCurrentVersion()
    {
        var collectorSet = new UsageCollectorSet();
        foreach (var collector in collectorSet.Collectors)
        {
            Assert.True(collector.VersionRequired <= UsageCollectorSet.CurrentVersion,
                        $"The version of {collector.GetType()} cannot exceed the current version {UsageCollectorSet.CurrentVersion}");
        }
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