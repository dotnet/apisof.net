global using static ApisOfDotNet.Shared.CatalogThrowHelpers;
using System.Runtime.CompilerServices;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public static class CatalogThrowHelpers
{
    public static void ThrowIfDefault(ApiModel argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument == default)
            throw new ArgumentException("Uninitialized API isn't valid", paramName);
    }
    
    public static void ThrowIfDefault(ExtensionMethodModel argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument == default)
            throw new ArgumentException("Uninitialized extension method isn't valid", paramName);
    }
}