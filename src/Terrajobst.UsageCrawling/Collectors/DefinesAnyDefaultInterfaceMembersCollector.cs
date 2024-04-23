using Microsoft.Cci;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DefinesAnyDefaultInterfaceMembersCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 2;

    protected override void CollectFeatures(IAssembly assembly, AssemblyContext assemblyContext, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            if (!type.IsInterface) continue;

            foreach (var member in type.Members)
            {
                if (member is IMethodDefinition { Body: not (null or Dummy) })
                {
                    context.Report(FeatureUsage.DefinesAnyDefaultInterfaceMembers);
                    return;
                }
            }
        }
    }
}