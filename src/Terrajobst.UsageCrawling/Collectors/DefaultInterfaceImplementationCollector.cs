using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefaultInterfaceImplementationCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 4;

    protected override void CollectFeatures(IAssembly assembly, AssemblyContext assemblyContext, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            if (!type.IsInterface) continue;

            foreach (var implementation in type.ExplicitImplementationOverrides)
            {
                if (implementation.ImplementedMethod.IsDefinedInCurrentAssembly())
                    continue;

                var docId = implementation.ImplementedMethod.UnWrapMember().DocId();
                var key = new ApiKey(docId);
                var metric = FeatureUsage.ForDim(key);
                context.Report(metric);
            }
        }
    }
}
