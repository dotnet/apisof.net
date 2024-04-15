using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefinesAnyDefaultInterfaceMembersCollector : IncrementalUsageCollector
{
    public override int VersionIntroduced => 2;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            if (!type.IsInterface) continue;

            foreach (var member in type.Members)
            {
                if (member is IMethodDefinition method && method.Body is not Dummy)
                {
                    context.Report(FeatureUsage.DefinesAnyDefaultInterfaceMembers);
                    return;
                }
            }
        }
    }
}