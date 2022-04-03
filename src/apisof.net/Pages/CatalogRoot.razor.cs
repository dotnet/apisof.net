
using ApiCatalog.Services;
using Microsoft.AspNetCore.Components;

namespace ApiCatalog.Pages;

public partial class CatalogRoot
{
    [Inject]
    public CatalogService CatalogService { get; set; }
}