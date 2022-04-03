using ApiCatalog.CatalogModel;
using ApiCatalog.Services;
using Microsoft.AspNetCore.Components;

namespace ApiCatalog.Pages;

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