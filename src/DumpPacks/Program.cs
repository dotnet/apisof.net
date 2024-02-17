var path = @"C:\\Program Files\\dotnet\\sdk-manifests\\";

foreach (var versionDirectory in Directory.GetDirectories(path))
{
    var versionText = Path.GetFileName(versionDirectory);

    var version = Version.Parse(versionText);
    Console.WriteLine($"** .NET {version.Major}.{version.Minor} **");
        
    var environment = await WorkloadEnvironment.LoadAsync(versionDirectory);
    var packByName = new Dictionary<string, Pack>(StringComparer.OrdinalIgnoreCase);

    foreach (var (_, workload) in environment.Workloads)
    {
        foreach (var packName in workload.Packs)
        {
            if (!environment.Packs.TryGetValue(packName, out var pack))
            {
                Console.WriteLine($"warning: Can't find pack '{packName}'");
                continue;
            }

            packByName.TryAdd(packName, pack);
        }
    }

    foreach (var (packName, pack) in packByName)
    {
        if (pack.Kind is not (PackKind.Library or PackKind.Framework))
            continue;
            
        if (packName.Contains(".Runtime.", StringComparison.OrdinalIgnoreCase))
            continue;
            
        Console.WriteLine($"{packName}, {pack.Version} ({pack.Kind})");
    }
}