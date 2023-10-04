using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using NetUpgradePlanner.Services;
using NetUpgradePlanner.ViewModels.AssemblyListView;

namespace NetUpgradePlanner.Views;

internal sealed partial class AssemblyListView : UserControl
{
    private readonly WorkspaceService _workspaceService;
    private readonly GraphView _graphView;
    private readonly AssemblyContextMenuService _assemblyContextMenuService;

    public AssemblyListView(WorkspaceService workspaceService,
                            GraphView graphView,
                            AssemblyListViewModel viewModel,
                            AssemblyContextMenuService assemblyContextMenuService)
    {
        InitializeComponent();

        _workspaceService = workspaceService;
        _graphView = graphView;
        _assemblyContextMenuService = assemblyContextMenuService;
        DataContext = viewModel;
    }

    private async void AssembliesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (AssembliesDataGrid.SelectedItems.Count > 0)
            {
                var selectedEntries = AssembliesDataGrid.SelectedItems
                                                        .OfType<AssemblyViewModel>()
                                                        .Select(e => e.Entry);
                await _workspaceService.RemoveAssembliesAsync(selectedEntries);
            }
        }
    }

    private void AssembliesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (AssembliesDataGrid.SelectedItem is AssemblyViewModel assembly)
            _graphView.ButterflyEntry = assembly.Entry;
    }

    private void AssemblyContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        _assemblyContextMenuService.Fill(AssemblyContextMenu.Items);
    }
}
