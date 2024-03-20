using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;

public sealed class WorkloadEnvironment
{
    private WorkloadEnvironment(IReadOnlyList<WorkloadManifest> manifests)
    {
        var workloadByName = new Dictionary<string, Workload>(StringComparer.OrdinalIgnoreCase);
        var packByName = new Dictionary<string, Pack>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifests)
        {
            foreach (var (name, workload) in manifest.Workloads)
                workloadByName.Add(name, workload);

            foreach (var (name, pack) in manifest.Packs)
                packByName.Add(name, pack);
        }

        Manifests = manifests;
        Workloads = workloadByName.ToFrozenDictionary();
        Packs = packByName.ToFrozenDictionary();

        foreach (var (key, value) in Workloads)
            value.Name = key;

        foreach (var (key, value) in Packs)
            value.Name = key;
    }

    public IReadOnlyList<WorkloadManifest> Manifests { get; }

    public FrozenDictionary<string, Workload> Workloads { get; }

    public FrozenDictionary<string, Pack> Packs { get; }

    public static async Task<WorkloadEnvironment> LoadAsync(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = {
                new JsonStringEnumConverter()
            }
        };

        var manifests = new List<WorkloadManifest>();

        foreach (var workloadDirectory in Directory.GetDirectories(path))
        {
            var manifestPath = ResolveManifestPath(workloadDirectory);
            if (manifestPath is null)
            {
                Console.WriteLine($"warning: can't manifest in {workloadDirectory}");
                continue;
            }
            
            await using var manifestStream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<WorkloadManifest>(manifestStream, options);
            if (manifest is not null)
                manifests.Add(manifest);
        }

        return new WorkloadEnvironment(manifests);

        static string? ResolveManifestPath(string workloadDirectory)
        {
            const string manifestName = "WorkloadManifest.json";

            var versionlessPath = Path.Join(workloadDirectory, manifestName);
            if (File.Exists(versionlessPath))
                return versionlessPath;
            
            var versionDirectories = Directory.GetDirectories(workloadDirectory);
            if (versionDirectories.Length == 0)
            {
                Console.WriteLine($"warning: can't find version directories in {workloadDirectory}");
                return null;
            }

            var highestVersion = versionDirectories.Select(p => (Path: p, Version: ParseVersionOrNull(p)))
                                                   .Where(t => t.Version is not null)
                                                   .OrderBy(t => t.Version)
                                                   .LastOrDefault();

            if (highestVersion != default)
            {
                var highestVersionPath = Path.Join(highestVersion.Path, "WorkloadManifest.json");
                if (File.Exists(highestVersionPath))
                    return highestVersionPath;
            }

            return null;
        }
        
        static NuGetVersion? ParseVersionOrNull(string path)
        {
            var fileName = Path.GetFileName(path);
            return !NuGetVersion.TryParse(fileName, out var result) ? null : result;
        }
    }

    public IReadOnlyList<(Pack, HashSet<Workload> Workloads)> GetFlattenedPacks()
    {
        var workloadExtenders = new Dictionary<Workload, HashSet<Workload>>();

        foreach (var derivedWorkload in Workloads.Values)
        {
            workloadExtenders.Add(derivedWorkload, new HashSet<Workload>());
        }

        foreach (var derivedWorkload in Workloads.Values)
        {
            foreach (var baseName in derivedWorkload.Extends)
            {
                if (!Workloads.TryGetValue(baseName, out var baseWorkload))
                {
                    Console.WriteLine($"error: Can't resolve base workload '{baseName}' from '{derivedWorkload.Name}'");
                    continue;
                }

                var extenders = workloadExtenders[baseWorkload];
                extenders.Add(derivedWorkload);
            }
        }

        foreach (var (_, extenders) in workloadExtenders)
        {
            NextRound:

            foreach (var e in extenders)
            {
                var modified = false;

                foreach (var d in workloadExtenders[e])
                {
                    if (extenders.Add(d))
                        modified = true;
                }

                if (modified)
                    goto NextRound;
            }
        }

        var packWorkloads = new Dictionary<Pack, HashSet<Workload>>();

        foreach (var workload in Workloads.Values)
        {
            foreach (var packName in workload.Packs)
            {
                if (!Packs.TryGetValue(packName, out var pack))
                {
                    Console.WriteLine($"error: Can't resolve pack '{packName}' from '{workload.Name}'");
                    continue;
                }

                if (!packWorkloads.TryGetValue(pack, out var workloads))
                {
                    workloads = new HashSet<Workload>();
                    packWorkloads.Add(pack, workloads);
                }

                workloads.Add(workload);
                workloads.UnionWith(workloadExtenders[workload]);
            }
        }

        foreach (var workloads in packWorkloads.Values)
        {
            workloads.RemoveWhere(w => w.Abstract);
        }

        return packWorkloads.Select(kv => (kv.Key, kv.Value))
                            .OrderBy(t => t.Key.Name)
                            .ToArray();
    }
}