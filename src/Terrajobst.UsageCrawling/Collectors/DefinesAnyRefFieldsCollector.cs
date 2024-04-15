using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefinesAnyRefFieldsCollector : IncrementalUsageCollector
{
    public override int VersionIntroduced => 3;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        foreach (var field in type.Fields)
        {
            if (field.Type is not IManagedPointerTypeReference)
                continue;

            context.Report(FeatureUsage.DefinesAnyRefFields);
            return;
        }
    }
}