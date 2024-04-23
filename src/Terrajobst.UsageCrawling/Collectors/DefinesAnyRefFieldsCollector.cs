using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefinesAnyRefFieldsCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 3;

    protected override void CollectFeatures(IAssembly assembly, AssemblyContext assemblyContext, Context context)
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