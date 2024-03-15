using System.Collections.Frozen;
using System.Collections.ObjectModel;

public sealed class WorkloadManifest
{
    public WorkloadManifest(string version,
                            IReadOnlyDictionary<string, Workload>? workloads,
                            IReadOnlyDictionary<string, Pack>? packs)
    {
        workloads ??= ReadOnlyDictionary<string, Workload>.Empty;
        packs ??= ReadOnlyDictionary<string, Pack>.Empty;

        Version = version;
        Workloads = workloads.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        Packs = packs.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public string Version { get; }

    public IReadOnlyDictionary<string, Workload> Workloads { get; }

    public IReadOnlyDictionary<string, Pack> Packs { get; }
}