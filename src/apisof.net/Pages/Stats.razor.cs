using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class Stats
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    public ApiCatalogStatistics Statistics => CatalogService.CatalogStatistics;
}