using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;

namespace ApisOfDotNet.Pages;

public partial class Diff
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required LinkService Link { get; set; }

    public NuGetFramework? Left { get; set; }

    public NuGetFramework? Right { get; set; }

    public bool HasDiff { get; set; }

    protected override void OnInitialized()
    {
        var query = BrowsingQuery.Get(CatalogService.Catalog, NavigationManager);

        if (query.Diff is not null)
        {
            HasDiff = true;
            Left = query.Diff.Value.Left;
            Right = query.Diff.Value.Right;
        }
        else
        {
            HasDiff = false;
            var latestTwoCoreVersions = CatalogService.Catalog.Frameworks
                .Select(fx => fx.NuGetFramework)
                .Where(fx => string.Equals(fx.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
                             !fx.HasPlatform)
                .OrderByDescending(fx => fx.Version)
                .Take(2)
                .ToArray();

            if (latestTwoCoreVersions.Length == 2)
            {
                Left = latestTwoCoreVersions[1];
                Right = latestTwoCoreVersions[0];
            }
        }
    }

    private void Browse()
    {
        if (Left is null || Right is null)
            return;

        var link = Link.ForCatalog(Left, Right);
        NavigationManager.NavigateTo(link);
    }

    private void DisableDiff()
    {
        var link = Link.ForCatalog(useBlankQuery: true);
        NavigationManager.NavigateTo(link);
    }
}