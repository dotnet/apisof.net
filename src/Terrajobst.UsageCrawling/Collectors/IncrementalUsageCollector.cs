using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public abstract class IncrementalUsageCollector : UsageCollector
{
    private readonly HashSet<FeatureUsage> _features = new();

    public sealed override void Collect(IAssembly assembly, AssemblyContext assemblyContext)
    {
        ThrowIfNull(assembly);
        ThrowIfNull(assemblyContext);

        var context = new Context(_features);
        CollectFeatures(assembly, assemblyContext, context);
    }

    public sealed override IEnumerable<FeatureUsage> GetResults()
    {
        return _features;
    }

    protected abstract void CollectFeatures(IAssembly assembly, AssemblyContext assemblyContext, Context context);

    protected readonly struct Context
    {
        private readonly HashSet<FeatureUsage> _receiver;

        internal Context(HashSet<FeatureUsage> receiver)
        {
            ThrowIfNull(receiver);

            _receiver = receiver;
        }

        public void Report(FeatureUsage feature)
        {
            ThrowIfNull(feature);

            _receiver.Add(feature);
        }
    }
}