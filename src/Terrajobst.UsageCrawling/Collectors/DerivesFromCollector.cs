using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class DerivesFromCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 4;

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

    private static void ReportContext(Context context, ITypeReference? baseType)
    {
        if (baseType is null or Dummy)
            return;

        if (baseType.IsDefinedInCurrentAssembly())
            return;

        var docId = baseType.UnWrap().DocId();
        context.Report(FeatureUsage.ForDerivesFrom(docId));
    }
}