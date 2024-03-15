using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        Workloads = workloadByName.ToFrozenDictionary();
        Packs = packByName.ToFrozenDictionary();

        foreach (var (key, value) in Workloads)
            value.Name = key;

        foreach (var (key, value) in Packs)
            value.Name = key;
    }

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

        var manifestPaths = Directory.GetFiles(path, "WorkloadManifest.json", SearchOption.AllDirectories);
        var manifests = new List<WorkloadManifest>();

        foreach (var manifestPath in manifestPaths)
        {
            await using var manifestStream = File.OpenRead(manifestPath);

            var manifest = await JsonSerializer.DeserializeAsync<WorkloadManifest>(manifestStream, options);
            if (manifest is not null)
                manifests.Add(manifest);
        }

        return new WorkloadEnvironment(manifests);
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