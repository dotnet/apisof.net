using Microsoft.Cci;
using Microsoft.Cci.Extensions;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class UsesNullableReferenceTypesCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 3;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        var referencedTypes = assembly.GetTypeReferences();
        var embeddedTypes = assembly.GetAllTypes();

        foreach (var typeReferences in referencedTypes.Concat(embeddedTypes))
        {
            if (IndicatesNullableUsage(typeReferences))
            {
                context.Report(FeatureUsage.UsesNullableReferenceTypes);
                return;
            }
        }
    }

    private static bool IndicatesNullableUsage(ITypeReference attributeType)
    {
        return attributeType is INamedTypeReference type &&
               type.Name.Value is "NullableAttribute" or "NullableContextAttribute" or "NullablePublicOnlyAttribute" &&
               type.GetNamespaceName() == "System.Runtime.CompilerServices";
    }
}