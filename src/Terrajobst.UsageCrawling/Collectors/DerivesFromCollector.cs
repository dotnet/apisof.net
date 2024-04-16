using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DerivesFromCollector : IncrementalUsageCollector
{
    public override int VersionIntroduced => 3;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            var baseClass = type.BaseClasses.SingleOrDefault();
            ReportContext(context, baseClass);

            foreach (var @interface in type.Interfaces)
                ReportContext(context, @interface);
        }
    }

    private static void ReportContext(Context context, ITypeReference? @interface)
    {
        if (@interface is null or Dummy)
            return;

        var docId = @interface.UnWrap().DocId();
        context.Report(FeatureUsage.ForDerivesFrom(docId));
    }
}