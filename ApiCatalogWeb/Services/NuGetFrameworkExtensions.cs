using System;

using NuGet.Frameworks;

namespace ApiCatalogWeb.Services
{
    public static class NuGetFrameworkExtensions
    {
        public static string GetVersionDisplayString(this Version version)
        {
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
            if (framework.HasProfile)
            {
                if (framework.IsPCL)
                    return framework.GetShortFolderName();

                return $"{framework.Version.GetVersionDisplayString()} ({framework.Profile})";
            }

            return framework.Version.GetVersionDisplayString();
        }

        public static string GetFrameworkDisplayString(this NuGetFramework framework)
        {
            switch (framework.Framework)
            {
                case ".NETFramework":
                    return ".NET Framework";
                case ".NETCoreApp":
                    return ".NET Core";
                case ".NETStandard":
                    return ".NET Standard";
                default:
                    return framework.Framework;
            }
        }
    }
}
