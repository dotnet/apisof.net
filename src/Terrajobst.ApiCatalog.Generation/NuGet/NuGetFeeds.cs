namespace Terrajobst.ApiCatalog;

public static class NuGetFeeds
{
    public static string NuGetOrg => "https://api.nuget.org/v3/index.json";
    public static string NightlyDotnet7 => "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json";
    public static string NightlyDotnet8 => "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json";
    public static string NightlyDotnet9 => "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json";
    public static string NightlyXamarin => "https://pkgs.dev.azure.com/azure-public/vside/_packaging/xamarin-impl/nuget/v3/index.json";
    public static string NightlyLatest => NightlyDotnet9;
}