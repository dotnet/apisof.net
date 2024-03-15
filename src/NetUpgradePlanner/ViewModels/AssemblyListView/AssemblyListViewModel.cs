using NetUpgradePlanner.Mvvm;
using NetUpgradePlanner.Services;

using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.ViewModels.AssemblyListView;

internal sealed class AssemblyListViewModel : ViewModel
{
    private readonly WorkspaceService _workspaceService;
    private readonly IconService _iconService;
    private IReadOnlyList<AssemblyViewModel> _assemblies = Array.Empty<AssemblyViewModel>();
    private AssemblyViewModel? _selectedAssembly;

    public AssemblyListViewModel(WorkspaceService workspaceService,
                                 IconService iconService)
    {
        _workspaceService = workspaceService;
        _workspaceService.Changed += WorkspaceService_Changed;
        _iconService = iconService;
    }

    public AssemblyViewModel? SelectedAssembly
    {
        get => _selectedAssembly;
        set
        {
            if (_selectedAssembly != value)
            {
                _selectedAssembly = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<AssemblyViewModel> Assemblies
    {
        get => _assemblies;
        set
        {
            if (_assemblies != value)
            {
                _assemblies = value;
                OnPropertyChanged();
            }
        }
    }

    private void WorkspaceService_Changed(object? sender, EventArgs e)
    {
        var report = _workspaceService.Current.Report;

        var reportDataByName = report is null
            ? new Dictionary<string, AnalyzedAssembly>()
            : report.AnalyzedAssemblies.ToDictionary(p => p.Entry.Name);

        var assemblyConfiguration =_workspaceService.Current.AssemblyConfiguration;

        Assemblies = _workspaceService.Current.AssemblySet.Entries.Select(e => CreateViewModel(e, reportDataByName)).ToArray();

        AssemblyViewModel CreateViewModel(AssemblySetEntry entry, Dictionary<string, AnalyzedAssembly> reportDataByName)
        {
            reportDataByName.TryGetValue(entry.Name, out var reportData);
            var icon = _iconService.GetIcon(IconKind.Assembly);
            var desiredFramework = assemblyConfiguration.GetDesiredFramework(entry);
            var desiredPlatforms = assemblyConfiguration.GetDesiredPlatforms(entry);
            return new AssemblyViewModel(icon, entry, desiredFramework, desiredPlatforms, reportData);
        }
    }
}
