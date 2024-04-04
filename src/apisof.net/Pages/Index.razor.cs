using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class Index
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [CascadingParameter]
    public required MainLayout MainLayout { get; set; }
   
    public ApiCatalogStatistics Statistics => CatalogService.CatalogStatistics;
}