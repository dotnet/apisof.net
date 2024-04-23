using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class ExceptionCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 4;

    protected override void CollectFeatures(IAssembly assembly, AssemblyContext assemblyContext, Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        foreach (var method in type.Methods)
        {
            if (method.Body is null or Dummy)
                continue;

            IOperation? previousOp = null;

            foreach (var op in method.Body.Operations)
            {
                if (op.OperationCode == OperationCode.Throw &&
                    previousOp is { OperationCode: OperationCode.Newobj, Value: IMethodReference ctor })
                {
                    if (!ctor.IsDefinedInCurrentAssembly())
                    {
                        var docId = ctor.UnWrapMember().DocId();
                        context.Report(FeatureUsage.ForExceptionThrow(docId));
                    }
                }

                if (op.OperationCode != OperationCode.Nop)
                    previousOp = op;
            }

            foreach (var i in method.Body.OperationExceptionInformation)
            {
                if (i.ExceptionType.IsDefinedInCurrentAssembly())
                    continue;

                var docId = i.ExceptionType.UnWrap().DocId();
                context.Report(FeatureUsage.ForExceptionCatch(docId));
            }
        }
    }
}