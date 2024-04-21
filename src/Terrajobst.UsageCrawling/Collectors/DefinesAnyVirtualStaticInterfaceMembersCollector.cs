using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefinesAnyVirtualStaticInterfaceMembersCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 2;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            if (!type.IsInterface)
                continue;

            foreach (var member in type.Members)
            {
                if (member is not IMethodDefinition method)
                    continue;

                if (!method.IsStatic || !method.IsVirtual)
                    continue;

                context.Report(FeatureUsage.DefinesAnyVirtualStaticInterfaceMembers);
                return;
            }
        }
    }
}