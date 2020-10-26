namespace ApiCatalog
{
    public static class NuGetFeeds
    {
        public static string NuGetOrg => "https://api.nuget.org/v3/index.json";
        public static string NightlyDotnet5 => "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json";
        public static string NightlyDotnet6 => "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json";
        public static string NightlyXamarin => "https://pkgs.dev.azure.com/azure-public/vside/_packaging/xamarin-impl/nuget/v3/index.json";
    }
}
