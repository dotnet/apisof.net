using System.Collections.Generic;
using System.Linq;

namespace NetUpgradePlanner.Analysis;

internal sealed class AnalyzedAssembly
{
    public AnalyzedAssembly(AssemblySetEntry entry,
                            float score,
                            IEnumerable<Problem> problems)
    {
        Entry = entry;
        Score = score;
        Problems = problems.ToArray();
    }

    public AssemblySetEntry Entry { get; }
    public float Score { get; }
    public IReadOnlyList<Problem> Problems { get; }
}
