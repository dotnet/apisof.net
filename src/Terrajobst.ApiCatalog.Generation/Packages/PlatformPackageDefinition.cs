using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Terrajobst.ApiCatalog;

internal static class PlatformPackageDefinition
{
    private static FrozenSet<string>? _packageIds;

    public static FrozenSet<string> Owners = FrozenSet.ToFrozenSet(
    [
        "aspnet",
        "dotnetframework",
        "dotnetiot",
        "EntityFramework",
        "RoslynTeam",
        "nugetsqltools",
        //"dotnetfoundation",
        "newtonsoft",
        "xamarin",
        "corewcf",
        "aspire",
        "MicrosoftReunionESTeam",
        "azure-sdk"
    ], StringComparer.OrdinalIgnoreCase);

    public static PackageFilter Filter { get; } = new(
        includes:
        [
                PackageFilterExpression.Parse("EntityFramework.*"),
                PackageFilterExpression.Parse("FSharp.*"),
                PackageFilterExpression.Parse("Microsoft.AspNet.*"),
                PackageFilterExpression.Parse("Microsoft.AspNetCore.*"),
                PackageFilterExpression.Parse("Microsoft.Bcl.*"),
                PackageFilterExpression.Parse("Microsoft.Build.*"),
                PackageFilterExpression.Parse("Microsoft.CodeAnalysis.*"),
                PackageFilterExpression.Parse("Microsoft.CodeDom.*"),
                PackageFilterExpression.Parse("Microsoft.CompilerServices.AsyncTargetingPack"),
                PackageFilterExpression.Parse("Microsoft.CSharp.*"),
                PackageFilterExpression.Parse("Microsoft.Data.*"),
                PackageFilterExpression.Parse("Microsoft.Diagnostics.*"),
                PackageFilterExpression.Parse("Microsoft.EntityFrameworkCore.*"),
                PackageFilterExpression.Parse("Microsoft.Extensions.*"),
                PackageFilterExpression.Parse("Microsoft.JSInterop.*"),
                PackageFilterExpression.Parse("Microsoft.Maui.*"),
                PackageFilterExpression.Parse("Microsoft.ML.*"),
                PackageFilterExpression.Parse("Microsoft.Net.Http.*"),
                PackageFilterExpression.Parse("Microsoft.ReverseProxy.*"),
                PackageFilterExpression.Parse("Microsoft.Spark.*"),
                PackageFilterExpression.Parse("Microsoft.VisualBasic.*"),
                PackageFilterExpression.Parse("Microsoft.Web.*"),
                PackageFilterExpression.Parse("Microsoft.Win32.*"),
                PackageFilterExpression.Parse("Microsoft.WindowsAppSDK"),
                PackageFilterExpression.Parse("System.*"),
                PackageFilterExpression.Parse("Iot.*"),
                PackageFilterExpression.Parse("Newtonsoft.Json.*"),
                PackageFilterExpression.Parse("CoreWCF.*"),
                PackageFilterExpression.Parse("Aspire.*"),
                PackageFilterExpression.Parse("Azure.*"),
        ],
        excludes:
        [
                PackageFilterExpression.Parse("*.cs"),
                PackageFilterExpression.Parse("*.de"),
                PackageFilterExpression.Parse("*.es"),
                PackageFilterExpression.Parse("*.fr"),
                PackageFilterExpression.Parse("*.it"),
                PackageFilterExpression.Parse("*.ja"),
                PackageFilterExpression.Parse("*.ko"),
                PackageFilterExpression.Parse("*.pl"),
                PackageFilterExpression.Parse("*.pt-br"),
                PackageFilterExpression.Parse("*.ru"),
                PackageFilterExpression.Parse("*.tr"),
                PackageFilterExpression.Parse("*.zh-Hans"),
                PackageFilterExpression.Parse("*.zh-Hant"),
        ]
    );

    public static FrozenSet<string> PackageIds
    {
        get
        {
            if (_packageIds is null)
            {
                using var stream = typeof(PlatformPackageDefinition).Assembly.GetManifestResourceStream("Terrajobst.ApiCatalog.Generation.Packages.PackageIds.txt")!;
                using var reader = new StreamReader(stream);
                var list = new List<string>();
                while (reader.ReadLine() is string line)
                    list.Add(line.Trim());

                var packageIds = list.ToFrozenSet();
                Interlocked.CompareExchange(ref _packageIds, packageIds, null);
            }

            return _packageIds;
        }
    }
}
