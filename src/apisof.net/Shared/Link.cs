using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public static class Link
{
    public static string For(ApiModel api)
    {
        return ForApi(api.Guid);
    }

    public static string For(ExtensionMethodModel extensionMethod)
    {
        return ForApi(extensionMethod.Guid);
    }

    private static string ForApi(Guid guid)
    {
        return $"/catalog/{guid:N}";
    }
}