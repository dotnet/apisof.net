using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public static class NuGetFrameworkExtensions
{
    public static bool IsRelevantForCatalog(this NuGetFramework framework)
    {
        ThrowIfNull(framework);

        return string.Equals(framework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(framework.Framework, ".NETStandard", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(framework.Framework, ".NETFramework", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetPlatformDisplayString(this NuGetFramework framework)
    {
        if (!framework.HasPlatform)
            return string.Empty;

        return PlatformAnnotationEntry.FormatPlatform(framework.Platform);
    }

    public static string GetPlatformVersionDisplayString(this NuGetFramework framework)
    {
        if (!framework.HasPlatform)
            return string.Empty;

        return framework.PlatformVersion.GetVersionDisplayString();
    }

    public static string GetVersionDisplayString(this Version version)
    {
        ThrowIfNull(version);

        var fieldCount = 4;
        if (version.Revision == 0)
        {
            fieldCount--;
            if (version.Build == 0)
                fieldCount--;
        }

        return version.ToString(fieldCount);
    }

    public static string GetVersionDisplayString(this NuGetFramework framework)
    {
        ThrowIfNull(framework);

        if (framework.IsPCL)
            return framework.GetShortFolderName();

        if (framework.HasProfile)
            return $"{framework.Version.GetVersionDisplayString()} ({framework.Profile})";

        if (!string.IsNullOrEmpty(framework.Platform))
        {
            if (framework.PlatformVersion is not null && framework.PlatformVersion > new Version(0, 0, 0, 0))
                return $"{framework.Version.GetVersionDisplayString()}-{framework.Platform}{framework.PlatformVersion.GetVersionDisplayString()}";

            return $"{framework.Version.GetVersionDisplayString()}-{framework.Platform}";
        }

        return framework.Version.GetVersionDisplayString();
    }

    public static string GetFrameworkDisplayString(this NuGetFramework framework)
    {
        ThrowIfNull(framework);

        switch (framework.Framework)
        {
            case ".NETFramework":
                return ".NET Framework";
            case ".NETCoreApp":
                if (framework.Version >= new Version(5, 0, 0, 0))
                    return ".NET";
                else
                    return ".NET Core";
            case ".NETStandard":
                return ".NET Standard";
            case "MonoAndroid":
                return "Xamarin Android";
            case "Xamarin.iOS":
                return "Xamarin iOS";
            case "Xamarin.Mac":
                return "Xamarin macOS";
            case "Xamarin.TVOS":
                return "Xamarin tvOS";
            case "Xamarin.WatchOS":
                return "Xamarin watchOS";
            default:
                return framework.Framework;
        }
    }

    public static NuGetFramework? GetBaseFramework(this NuGetFramework framework)
    {
        ThrowIfNull(framework);

        var hasPlatform = framework.HasPlatform;
        var hasNetCoreApp3Profile = string.Equals(framework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
                                    framework.Version.Major == 3 && framework.HasProfile;

        if (hasPlatform || hasNetCoreApp3Profile)
            return new NuGetFramework(framework.Framework, framework.Version);

        return null;
    }

    public static NuGetFramework GetBaseFrameworkOrSelf(this NuGetFramework framework)
    {
        return framework.GetBaseFramework() ?? framework;
    }

    public static bool CanHavePlatform(this NuGetFramework framework)
    {
        return string.Equals(framework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
               framework.Version.Major >= 3;
    }

    public static bool IsPlatformNeutral(this NuGetFramework framework)
    {
        return framework.CanHavePlatform() && !framework.HasPlatform && !framework.HasProfile;
    }

}