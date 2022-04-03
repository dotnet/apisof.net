using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class Stats
{
    [Inject]
    public CatalogService CatalogService { get; set; }

    public ApiCatalogStatistics Statistics { get; set; }

    protected override void OnInitialized()
    {
        Statistics = CatalogService.CatalogStatistics;
    }
}