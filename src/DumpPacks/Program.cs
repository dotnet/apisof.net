using NuGet.Versioning;

var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
var dotnetDirectory = Path.Join(programFiles, "dotnet");

Console.WriteLine("//");
Console.WriteLine("// Built-in Packs");
Console.WriteLine("//");

foreach (var sdkDirectory in GetSdkDirectories(dotnetDirectory))
{
    var version = NuGetVersion.Parse(Path.GetFileName(sdkDirectory));

    Console.WriteLine($"// .NET SDK {version}");

    var references = KnownFrameworkReference.Load(sdkDirectory);

    foreach (var frameworkGroup in references.GroupBy(f => f.TargetFramework)
                                             .OrderBy(g => g.Key.Framework)
                                             .ThenBy(g => g.Key.Version))
    {
        Console.WriteLine($"// {frameworkGroup.Key.GetShortFolderName()}");

        foreach (var packGroup in frameworkGroup.GroupBy(r => r.TargetingPackName)
                                                .OrderBy(p => p.Key))
        {
            var pack = packGroup.MaxBy(p => p.TargetingPackVersion)!;
            Console.WriteLine($"{pack.TargetingPackName}, {pack.TargetingPackVersion}");
        }
    }

    var supportedVersions = SupportedTargetPlatformVersion
        .Load(sdkDirectory)
        .GroupBy(v => v.Platform)
        .Select(g => (g.Key, string.Join(", ", g.Select(v => v.Version).Distinct().Order())));

    foreach (var (platform, versionList) in supportedVersions)
        Console.WriteLine($"{platform}: {versionList}");
}

Console.WriteLine("//");
Console.WriteLine("// Workload Packs");
Console.WriteLine("//");

var manifestsRoot = Path.Join(dotnetDirectory, "sdk-manifests");
var packsRoot = Path.Join(dotnetDirectory, "packs");

foreach (var versionDirectory in Directory.GetDirectories(manifestsRoot))
{
    var versionText = Path.GetFileName(versionDirectory);
    var version = NuGetVersion.Parse(versionText);

    Console.WriteLine($"// net{version.Major}.{version.Minor}");

    var environment = await WorkloadEnvironment.LoadAsync(versionDirectory);

    foreach (var (pack, workloads) in environment.GetFlattenedPacks())
    {
        if (pack.Kind is not (PackKind.Library or PackKind.Framework))
            continue;

        if (pack.Name.Contains(".Runtime.", StringComparison.OrdinalIgnoreCase))
            continue;

        var workloadNames = string.Join(", ", workloads.Select(w => w.Name).Order());

        if (!pack.AliasTo.Any())
        {
            Console.WriteLine($"{pack.Name}, {pack.Version} ({pack.Kind}): {workloadNames}");
        }
        else
        {
            foreach (var aliasTo in pack.AliasTo.Values.Distinct().Order())
                Console.WriteLine($"{aliasTo}, {pack.Version} ({pack.Kind}): {workloadNames}");
        }
    }

    var platformVersions = new Dictionary<string, SortedSet<Version>>(StringComparer.OrdinalIgnoreCase);

    foreach (var (pack, workloads) in environment.GetFlattenedPacks())
    {
        if (pack.Kind is not PackKind.Sdk)
            continue;

        var packNames = (string[]) [pack.Name, ..pack.AliasTo.Select(kv => kv.Value)];

        foreach (var packName in packNames.Distinct())
        {
            var sdkDirectory = Path.Join(packsRoot, packName, pack.Version);
            if (!Directory.Exists(sdkDirectory))
                continue;

            var supportedVersions = SupportedTargetPlatformVersion.Load(sdkDirectory).GroupBy(v => v.Platform);
            if (!supportedVersions.Any())
                continue;

            foreach (var platformGroup in supportedVersions)
            {
                var platform = platformGroup.Key;

                if (!platformVersions.TryGetValue(platform, out var versions))
                {
                    versions = new();
                    platformVersions.Add(platform, versions);
                }

                versions.UnionWith(platformGroup.Select(v => v.Version));
            }
        }
    }

    foreach (var (platform, versions) in platformVersions)
    {
        var versionList = string.Join(", ", versions);
        Console.WriteLine($"{platform}: {versionList}");
    }
}

static IReadOnlyList<string> GetSdkDirectories(string dotnetDirectory)
{
    var sdkRoot = Path.Join(dotnetDirectory, "sdk");
    return Directory.GetDirectories(sdkRoot)
                    .Select(d => (Path: d, Version: NuGetVersion.Parse(Path.GetFileName(d))))
                    .GroupBy(t => (t.Version.Major, t.Version.Minor))
                    .Select(g => g.OrderByDescending(t => t.Version).First())
                    .Select(t => t.Path)
                    .ToArray();
}
