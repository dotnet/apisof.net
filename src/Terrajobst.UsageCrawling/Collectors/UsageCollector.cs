using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public abstract class UsageCollector
{
    public abstract int VersionRequired { get; }

    public abstract void Collect(IAssembly assembly);

    public abstract IEnumerable<FeatureUsage> GetResults();
}