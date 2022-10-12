using NuGet.Frameworks;

using System.Diagnostics;

using Terrajobst.ApiCatalog;

var catalogFileName = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "catalog.bin");
var reportFileName = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "report.csv");

if (!File.Exists(catalogFileName))
    await ApiCatalogModel.DownloadFromWebAsync(catalogFileName);

var catalog = await ApiCatalogModel.LoadAsync(catalogFileName);

var frameworks = catalog.Frameworks.Select(x => NuGetFramework.Parse(x.Name))
                                   .ToArray();

var latestNetFx = frameworks.Where(fx => !fx.HasProfile && string.Equals(fx.Framework, ".NETFramework", StringComparison.OrdinalIgnoreCase))
                            .MaxBy(fx => fx.Version);

var latestNetCore = frameworks.Where(fx => string.Equals(fx.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(fx.Platform, "Windows", StringComparison.OrdinalIgnoreCase))
                              .MaxBy(fx => fx.Version);

Debug.Assert(latestNetFx is not null);
Debug.Assert(latestNetCore is not null);

var includeMatched = true;

var availabilityContext = ApiAvailabilityContext.Create(catalog);
var featureContext = MissingNetFxFeatureContext.Create();

using var writer = new CsvWriter(reportFileName);
writer.Write("Assembly");
writer.Write("Namespace");
writer.Write("Type");
writer.Write("Member");

if (includeMatched)
    writer.Write("Feature");

writer.WriteLine();

foreach (var api in catalog.GetAllApis())
{
    if (api.Kind == ApiKind.Namespace)
        continue;

    var netFxAvailability = availabilityContext.GetAvailability(api, latestNetFx);
    var netCoreAvailability = availabilityContext.GetAvailability(api, latestNetCore);

    if (netFxAvailability is null ||
        !netFxAvailability.IsInBox ||
        netCoreAvailability is not null)
        continue;

    if (netFxAvailability.Declaration.IsOverride())
        continue;

    var feature = featureContext.Get(netFxAvailability.Declaration);

    if (feature is null || includeMatched)
    {
        writer.Write(netFxAvailability.Declaration.Assembly.Name);
        writer.Write(api.GetNamespaceName());
        writer.Write(api.GetTypeName());
        writer.Write(api.GetMemberName());

        if (includeMatched)
        {
            if (feature is null)
                writer.Write(string.Empty);
            else
                writer.Write(feature.Name);
        }

        writer.WriteLine();
    }
}

var unsuedMatchers = featureContext.GetUnusedMatchers();
foreach (var unusedMatcher in unsuedMatchers)
    Console.WriteLine($"warn: unused matcher {unusedMatcher}");
