using NuGet.Versioning;

var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
if (string.IsNullOrEmpty(dotnetRoot))
    dotnetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"dotnet");
var dumpPackManifest = new DumpPackManifest();
// Console.WriteLine("//");
// Console.WriteLine("// Built-in Packs");
// Console.WriteLine("//");

foreach (var sdkDirectory in GetSdkDirectories(dotnetRoot))
{
    var version = NuGetVersion.Parse(Path.GetFileName(sdkDirectory));
    var builtInManifest = new BuiltInPackManifest
    {
        SdkVersion = $".NET SDK {version}"
    };
    // Console.WriteLine($"// .NET SDK {version}");

    var references = KnownFrameworkReference.Load(sdkDirectory);

    foreach (var frameworkGroup in references.GroupBy(f => f.TargetFramework)
                                             .OrderBy(g => g.Key.Framework)
                                             .ThenBy(g => g.Key.Version))
    {
        // Console.WriteLine($"// {frameworkGroup.Key.GetShortFolderName()}");
        var frameworkReferenceContent = new FrameworkReferenceContent
        {
            TargetFramework = frameworkGroup.Key.GetShortFolderName()
        };
        foreach (var packGroup in frameworkGroup.GroupBy(r => r.TargetingPackName)
                                                .OrderBy(p => p.Key))
        {
            var pack = packGroup.MaxBy(p => p.TargetingPackVersion)!;
            // Console.WriteLine($"{pack.TargetingPackName}, {pack.TargetingPackVersion}");
            var frameworkReferencePack = new FrameworkReferencePack
            {
                PackName = pack.TargetingPackName,
                PackVersion = pack.TargetingPackVersion.ToString()
            };
            frameworkReferenceContent.Packs.Add(frameworkReferencePack);
        }

        builtInManifest.FrameworkReferences.Add(frameworkReferenceContent);
    }

    var supportedVersions = SupportedTargetPlatformVersion
        .Load(sdkDirectory)
        .GroupBy(v => v.Platform)
        .Select(g => (g.Key, string.Join(", ", g.Select(v => v.Version).Distinct().Order())));

    foreach (var (platform, versionList) in supportedVersions)
    {
        
        var platformVersion = new PlatformVersion
        {
            Platform = platform,
            Versions = versionList.Split(", ").ToList()
        };
        builtInManifest.PlatformVersions.Add(platformVersion);
        
        // Console.WriteLine($"{platform}: {versionList}");
    }
    dumpPackManifest.BuiltInPackManifests.Add(builtInManifest);
}

// Console.WriteLine("//");
// Console.WriteLine("// Workload Packs");
// Console.WriteLine("//");

var manifestsRoot = Path.Join(dotnetRoot, "sdk-manifests");
var packsRoot = Path.Join(dotnetRoot, "packs");

foreach (var versionDirectory in Directory.GetDirectories(manifestsRoot))
{
    var versionText = Path.GetFileName(versionDirectory);
    var version = NuGetVersion.Parse(versionText);
    var workloadManifest = new WorkLoadPackManifest
    {
        DotNetVersion = $"net{version.Major}.{version.Minor}"
    };

    // Console.WriteLine($"// net{version.Major}.{version.Minor}");

    var environment = await WorkloadEnvironment.LoadAsync(versionDirectory);

    foreach (var (pack, workloads) in environment.GetFlattenedPacks())
    {
        if (pack.Kind is not (PackKind.Library or PackKind.Framework))
            continue;

        if (pack.Name.Contains(".Runtime.", StringComparison.OrdinalIgnoreCase))
            continue;

        if (!pack.AliasTo.Any())
        {
            var jsonContent = new WorkLoadPackContent
            {
                PackName = pack.Name,
                PackVersion = pack.Version,
                PackKind = pack.Kind.ToString(),
                WorkloadNames = workloads.Select(w => w.Name).Order().ToList()
            };
            workloadManifest.Packs.Add(jsonContent);
            // Console.WriteLine($"{pack.Name}, {pack.Version} ({pack.Kind}): {workloadNames}");
            //Console.WriteLine(SetJsonString(jsonContent));
        }
        else
        {
            foreach (var aliasTo in pack.AliasTo.Values.Distinct().Order())
            {
                var jsonContent = new WorkLoadPackContent
                {
                    PackName = aliasTo,
                    PackVersion = pack.Version,
                    PackKind = pack.Kind.ToString(),
                    WorkloadNames = workloads.Select(w => w.Name).Order().ToList()
                };
                workloadManifest.Packs.Add(jsonContent);
                // Console.WriteLine($"{aliasTo}, {pack.Version} ({pack.Kind}): {workloadNames}");
                //Console.WriteLine(SetJsonString(jsonContent));
            }
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

        var platformVersion = new PlatformVersion
        {
            Platform = platform,
            Versions = versions.Select(v => v.ToString()).Order().ToList()
        };
        workloadManifest.PlatformVersions.Add(platformVersion);
        // Console.WriteLine($"{platform}: {versionList}");
    }
    dumpPackManifest.WorkLoadPackManifests.Add(workloadManifest);

}

dumpPackManifest.Errors = DumpPackDiagnostics.Drain();
// Output the final manifest as JSON
Console.WriteLine(SetJsonString(dumpPackManifest));

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

static string SetJsonString(object value)
{
    return System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
}