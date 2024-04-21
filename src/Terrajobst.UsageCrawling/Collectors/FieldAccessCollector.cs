using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class FieldAccessCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 3;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        foreach (var method in type.Methods)
        {
            if (method.Body is null or Dummy)
                continue;

            foreach (var op in method.Body.Operations)
            {
                switch (op.OperationCode)
                {
                    case OperationCode.Ldsfld:
                    case OperationCode.Ldfld:
                        ReportReadOrWrite(context, op.Value, isRead: true);
                        break;
                    case OperationCode.Stsfld:
                    case OperationCode.Stfld:
                        ReportReadOrWrite(context, op.Value, isRead: false);
                        break;
                }
            }
        }
    }

    private static void ReportReadOrWrite(Context context, object opValue, bool isRead)
    {
        var field = opValue as IFieldReference;
        if (field is null or Dummy)
            return;

        var docId = field.UnWrapMember().DocId();
        var featureUsage = isRead
            ? FeatureUsage.ForFieldRead(docId)
            : FeatureUsage.ForFieldWrite(docId);

        context.Report(featureUsage);
    }
}