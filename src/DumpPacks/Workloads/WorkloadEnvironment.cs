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
    }

    public FrozenDictionary<string,Workload> Workloads { get; }

    public FrozenDictionary<string,Pack> Packs { get; }
    
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
}