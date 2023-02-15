namespace Terrajobst.NetUpgradePlanner;

public sealed class Workspace
{
    public static Workspace Default { get; } = new Workspace();

    private Workspace()
    {
        AssemblySet = AssemblySet.Empty;
        AssemblyConfiguration = AssemblyConfiguration.Empty;
    }

    public Workspace(AssemblySet assemblySet,
                     AssemblyConfiguration assemblyConfiguration,
                     AnalysisReport? report)
    {
        AssemblySet = assemblySet;
        AssemblyConfiguration = assemblyConfiguration;
        Report = report;
    }

    public AssemblySet AssemblySet { get; }

    public AssemblyConfiguration AssemblyConfiguration { get; }

    public AnalysisReport? Report { get; }

    public bool InputChanged(Workspace other)
    {
        return AssemblySet != other.AssemblySet ||
               AssemblyConfiguration != other.AssemblyConfiguration;
    }
}
