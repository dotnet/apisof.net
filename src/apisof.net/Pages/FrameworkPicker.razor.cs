using System.Collections.Frozen;
using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class FrameworkPicker
{
    private string? _selectedFrameworkName;
    private NuGetFramework? _selected;
    private string? _selectedFamily;

    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Parameter]
    public NuGetFramework? Selected
    {
        get
        {
            return _selected;
        }
        set
        {
            if (_selected != value)
            {
                _selected = value;
                SelectedChanged.InvokeAsync(Selected);
            }
        }
    }

    [Parameter]
    public EventCallback<NuGetFramework?> SelectedChanged { get; set; }

    public string? SelectedFamily
    {
        get
        {
            return _selectedFamily;
        }
        set
        {
            if (_selectedFamily != value)
            {
                _selectedFamily = value;
                if (value is not null && FrameworksByFamily.TryGetValue(value, out var frameworks) &&
                    frameworks.Length > 0)
                {
                    SelectedFrameworkName = frameworks.First().GetShortFolderName();
                }
            }
        }
    }

    public string? SelectedFrameworkName
    {
        get => _selectedFrameworkName;
        set
        {
            if (_selectedFrameworkName != value)
            {
                _selectedFrameworkName = value;
                UpdateSelected();
            }
        }
    }

    public IReadOnlyList<string> FrameworkFamilies { get; set; } = Array.Empty<string>();

    public FrozenDictionary<string, NuGetFramework[]> FrameworksByFamily { get; set; } = FrozenDictionary<string, NuGetFramework[]>.Empty;

    protected override void OnParametersSet()
    {
        var frameworks = CatalogService.Catalog.Frameworks.Select(fx => NuGetFramework.Parse(fx.Name)).ToArray();

        FrameworkFamilies = frameworks.Select(fx => fx.GetFrameworkDisplayString()).ToHashSet().Order().ToArray();
        FrameworksByFamily = frameworks.GroupBy(fx => fx.GetFrameworkDisplayString())
                                       .Select(g => (Family: g.Key, Frameworks: g.OrderByDescending(fx => fx.Version)
                                                                                 .ThenBy(fx => fx.Platform)
                                                                                 .ThenByDescending(fx => fx.PlatformVersion)
                                                                                 .ToArray()))
                                       .ToFrozenDictionary(t => t.Family, t => t.Frameworks);

        Selected ??= FrameworksByFamily[FrameworkFamilies.First()].First();

        if (Selected is not null)
        {
            var selected = Selected;
            SelectedFamily = selected.GetFrameworkDisplayString();
            SelectedFrameworkName = selected.GetShortFolderName();
        }
    }

    private void UpdateSelected()
    {
        Selected = string.IsNullOrEmpty(SelectedFrameworkName)
            ? null
            : NuGetFramework.Parse(SelectedFrameworkName);
    }
}