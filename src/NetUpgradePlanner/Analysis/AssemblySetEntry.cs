using System;
using System.Collections.Generic;
using System.Linq;

namespace NetUpgradePlanner.Analysis;

internal sealed class AssemblySetEntry
{
    public AssemblySetEntry(string name,
                            string? targetFramework,
                            IEnumerable<string> dependencies,
                            IEnumerable<Guid> usedApis)
    {
        Fingerprint = Analysis.Fingerprint.Create(name);
        Name = name;
        TargetFramework = targetFramework;
        Dependencies = dependencies.ToArray();
        UsedApis = usedApis.ToArray();
    }

    public string Fingerprint { get; }

    public string Name { get; }

    public string? TargetFramework { get; }

    public IReadOnlyList<string> Dependencies { get; }

    public IReadOnlyList<Guid> UsedApis { get; }
}
