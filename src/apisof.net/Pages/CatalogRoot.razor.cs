using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class CatalogRoot
    : IDisposable
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required LinkService Link { get; set; }

    public ApiBrowsingContext BrowsingContext { get; set; } = ApiBrowsingContext.Empty;

    protected override void OnInitialized()
    {
        QueryManager.QueryChanged += QueryManagerOnQueryChanged;
        UpdateBrowsingContext();
    }

    private void UpdateBrowsingContext()
    {
        var query = BrowsingQuery.Get(CatalogService.Catalog, NavigationManager);
        var framework = query.Fx?.Framework;

        if (query.Diff is null)
        {
            BrowsingContext = ApiBrowsingContext.ForFramework(framework);
        }
        else
        {
            var left = query.Diff.Value.Left;
            var right = query.Diff.Value.Right;
            var diffOptions = query.DiffOptions?.DiffOptions;
            BrowsingContext = ApiBrowsingContext.ForFrameworkDiff(left, right, diffOptions, framework);
        }
    }

    private void QueryManagerOnQueryChanged(object? sender, IReadOnlySet<string> e)
    {
        UpdateBrowsingContext();
        StateHasChanged();
    }

    public void Dispose()
    {
        QueryManager.QueryChanged -= QueryManagerOnQueryChanged;
    }
}