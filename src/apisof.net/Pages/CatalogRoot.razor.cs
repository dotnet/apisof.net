using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;

namespace ApisOfDotNet.Pages;

public partial class CatalogRoot
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required LinkService Link { get; set; }

    public ApiBrowsingContext BrowsingContext { get; set; } = ApiBrowsingContext.Empty;

    protected override void OnInitialized()
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
            BrowsingContext = ApiBrowsingContext.ForFrameworkDiff(CatalogService.Catalog, left, right, framework);
        }
    }
}