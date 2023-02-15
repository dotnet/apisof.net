using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NetUpgradePlanner.ViewModels.AssemblyListView;
using NetUpgradePlanner.Views;
using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Services;

internal sealed class AssemblySelectionService
{
    private readonly AssemblyListView _assemblyListView;
    private readonly GraphView _graphView;

    public AssemblySelectionService(AssemblyListView assemblyListView,
                                    GraphView graphView)
    {
        _assemblyListView = assemblyListView;
        _assemblyListView.AssembliesDataGrid.SelectionChanged += AssembliesDataGrid_SelectionChanged;

        _graphView = graphView;
        _graphView.SelectionChanged += GraphView_SelectionChanged;
    }

    public AssemblySetEntry? GetSelectedAssembly()
    {
        if (_graphView.IsKeyboardFocusWithin)
        {
            return _graphView.SelectedEntry;
        }

        var assemblyViewModel = _assemblyListView.AssembliesDataGrid.SelectedItem as AssemblyViewModel;
        return assemblyViewModel?.Entry;
    }

    public IEnumerable<AssemblySetEntry> GetSelectedAssemblies()
    {
        if (_graphView.IsKeyboardFocusWithin)
        {
            if (_graphView.SelectedEntry is null)
                return Enumerable.Empty<AssemblySetEntry>();
            else
                return new[] { _graphView.SelectedEntry };
        }

        return _assemblyListView.AssembliesDataGrid
                                .SelectedItems
                                .OfType<AssemblyViewModel>()
                                .Select(vm => vm.Entry);
    }

    private void AssembliesDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedEntry = GetSelectedAssembly();
        _graphView.SelectedEntry = selectedEntry;

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void GraphView_SelectionChanged(object? sender, EventArgs e)
    {
        var selectedEntry = GetSelectedAssembly();
        var listViewModel = _assemblyListView.DataContext as AssemblyListViewModel;
        if (listViewModel is not null)
        {
            var assemblyViewModel = listViewModel.Assemblies.FirstOrDefault(vm => vm.Entry == selectedEntry);
            if (assemblyViewModel is not null)
            {
                _assemblyListView.AssembliesDataGrid.SelectedItem = assemblyViewModel;
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;
}
