using NuGet.Versioning;

var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
var dotnetDirectory = Path.Join(programFiles, "dotnet");

Console.WriteLine("//");
Console.WriteLine("// Built-in Packs");
Console.WriteLine("//");

var sdksRoot =Path.Join(dotnetDirectory, "sdk");

var references = KnownFrameworkReference.Load(sdksRoot);

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

Console.WriteLine("//");
Console.WriteLine("// Workload Packs");
Console.WriteLine("//");

var manifestsRoot = Path.Join(dotnetDirectory, "sdk-manifests");

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

        Console.WriteLine($"{pack.Name}, {pack.Version} ({pack.Kind}): {workloadNames}");
    }
}