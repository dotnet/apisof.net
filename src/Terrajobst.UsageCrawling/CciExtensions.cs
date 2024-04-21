using Microsoft.Cci;

namespace Terrajobst.UsageCrawling;

internal static class CciExtensions
{
    public static bool IsDefinedInCurrentAssembly(this ITypeReference reference)
    {
        ThrowIfNull(reference);
        return reference.ResolvedType is not Dummy;
    }

    public static bool IsDefinedInCurrentAssembly(this ITypeMemberReference reference)
    {
        ThrowIfNull(reference);
        return reference.ResolvedTypeDefinitionMember is not Dummy;
    }
}
