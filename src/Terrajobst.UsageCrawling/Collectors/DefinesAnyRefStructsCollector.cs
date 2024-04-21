using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefinesAnyRefStructsCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 2;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            if (!type.IsStruct) continue;

            foreach (var ca in type.Attributes)
            {
                if (string.Equals(ca.Type.FullName(), "System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparison.Ordinal))
                {
                    context.Report(FeatureUsage.DefinesAnyRefStructs);
                    return;
                }
            }
        }
    }
}