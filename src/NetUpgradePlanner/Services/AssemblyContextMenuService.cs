using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Extensions.DependencyInjection;
using NuGet.Frameworks;
using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Services;

internal sealed class AssemblyContextMenuService
{
    private readonly WorkspaceService _workspaceService;
    private readonly SelectFrameworkDialogService _selectFrameworkDialogService;
    private readonly SelectPlatformsDialogService _selectPlatformsDialogService;
    private readonly IServiceProvider _serviceProvider;

    public AssemblyContextMenuService(WorkspaceService workspaceService,
                                      SelectFrameworkDialogService selectFrameworkDialogService,
                                      SelectPlatformsDialogService selectPlatformsDialogService,
                                      IServiceProvider serviceProvider)
    {
        _workspaceService = workspaceService;
        _selectFrameworkDialogService = selectFrameworkDialogService;
        _selectPlatformsDialogService = selectPlatformsDialogService;       
        _serviceProvider = serviceProvider;
    }

    private IEnumerable<AssemblySetEntry> GetSelectedAssemblies()
    {
        var selectionService = _serviceProvider.GetRequiredService<AssemblySelectionService>();
        return selectionService.GetSelectedAssemblies();
    }

    public void Fill(ItemCollection target)
    {
        target.Clear();

        var setDesiredFrameworkMenuItem = new MenuItem
        {
            Header = "Set Desired Framework...",
            IsEnabled = GetSelectedAssemblies().Any()
        };
        setDesiredFrameworkMenuItem.Click += SetDesiredFrameworkMenuItem_Click;
        target.Add(setDesiredFrameworkMenuItem);

        var setDesiredPlatformsMenuItem = new MenuItem
        {
            Header = "Set Desired Platforms...",
            IsEnabled = GetSelectedAssemblies().Any()
        };
        setDesiredPlatformsMenuItem.Click += SetDesiredPlatformsMenuItem_Click;
        target.Add(setDesiredPlatformsMenuItem);

        target.Add(new Separator());

        var removeMenuItem = new MenuItem
        {
            Header = "Remove",
            IsEnabled = GetSelectedAssemblies().Any()
        };
        removeMenuItem.Click += RemoveMenuItem_Click;
        target.Add(removeMenuItem);
    }

    private async void SetDesiredFrameworkMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedEntries = GetSelectedAssemblies();

        var assembly = selectedEntries.First();
        var workspace = _workspaceService.Current;
        var framework = workspace.AssemblyConfiguration.GetDesiredFramework(assembly);
        var selectedFramework = await _selectFrameworkDialogService.SelectFrameworkAsync(framework);
        if (selectedFramework is not null)
            await _workspaceService.SetDesiredFrameworkAsync(selectedEntries, selectedFramework);
    }

    private async void SetDesiredPlatformsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedEntries = GetSelectedAssemblies();

        if (AnyHavePlatformSpecificFramework(selectedEntries))
        {
            var caption = "Cannot set platform targets";
            var message = "At least one selected assembly has a platform-specific framework " +
                          "and thus cannot set custom platform targets.";
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var assembly = selectedEntries.First();
        var workspace = _workspaceService.Current;
        var platforms = workspace.AssemblyConfiguration.GetDesiredPlatforms(assembly);
        var selectedPlatforms = await _selectPlatformsDialogService.SelectPlatformsAsync(platforms);
        if (selectedPlatforms is not null)
            await _workspaceService.SetDesiredPlatformsAsync(selectedEntries, selectedPlatforms.Value);
    }

    private async void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedEntries = GetSelectedAssemblies();
        await _workspaceService.RemoveAssembliesAsync(selectedEntries);
    }

    private bool AnyHavePlatformSpecificFramework(IEnumerable<AssemblySetEntry> selectedAssemblies)
    {
        var configuration = _workspaceService.Current.AssemblyConfiguration;

        foreach (var assembly in selectedAssemblies)
        {
            var desiredFramework = configuration.GetDesiredFramework(assembly);
            var framework = NuGetFramework.Parse(desiredFramework);
            if (framework.HasPlatform)
                return true;
        }

        return false;
    }
}