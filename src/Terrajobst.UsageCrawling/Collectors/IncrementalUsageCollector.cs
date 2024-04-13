using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public abstract class IncrementalUsageCollector : UsageCollector
{
    private readonly HashSet<UsageMetric> _features = new();

    public sealed override void Collect(IAssembly assembly)
    {
        ThrowIfNull(assembly);

        var context = new Context(_features);
        CollectFeatures(assembly, context);
    }

    public sealed override IEnumerable<UsageMetric> GetResults()
    {
        return _features;
    }

    protected abstract void CollectFeatures(IAssembly assembly, Context context);

    protected readonly struct Context
    {
        private readonly HashSet<UsageMetric> _receiver;

        internal Context(HashSet<UsageMetric> receiver)
        {
            ThrowIfNull(receiver);

            _receiver = receiver;
        }

        public void Report(UsageMetric feature)
        {
            ThrowIfNull(feature);

            _receiver.Add(feature);
        }
    }
}