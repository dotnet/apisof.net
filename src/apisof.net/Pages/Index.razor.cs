using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class Index
{
    [Inject]
    public CatalogService CatalogService { get; set; }

    public ApiCatalogStatistics Statistics => CatalogService.CatalogStatistics;
}