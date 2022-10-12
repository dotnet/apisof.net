using NetUpgradePlanner.Mvvm;
using NetUpgradePlanner.Analysis;
using System.Windows.Media;

namespace NetUpgradePlanner.ViewModels.AssemblyListView;

internal sealed class AssemblyViewModel : ViewModel
{
    public AssemblyViewModel(ImageSource? icon,
                             AssemblySetEntry entry,
                             string desiredFramework,
                             PlatformSet desiredPlatforms,
                             AnalyzedAssembly? reportData)
    {
        Icon = icon;
        Entry = entry;
        DesiredFramework = desiredFramework;
        DesiredPlatforms = desiredPlatforms.ToDisplayString();
        ReportData = reportData;
    }

    public ImageSource? Icon { get; }

    public AssemblySetEntry Entry { get; }

    public string DesiredFramework { get; }

    public string DesiredPlatforms { get; }

    public AnalyzedAssembly? ReportData { get; }

    public string Name => Entry.Name;

    public string? TargetFramework => Entry.TargetFramework;

    public float? PortingScore => ReportData?.Score;

    public int? Problems => ReportData?.Problems.Count;
}
