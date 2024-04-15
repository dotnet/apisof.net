using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefaultInterfaceImplementationCollector : IncrementalUsageCollector
{
    public override int VersionIntroduced => 2;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            if (!type.IsInterface) continue;

            foreach (var implementation in type.ExplicitImplementationOverrides)
            {
                var docId = implementation.ImplementedMethod.UnWrapMember().DocId();
                var key = new ApiKey(docId);
                var metric = FeatureUsage.ForDim(key);
                context.Report(metric);
            }
        }
    }
}