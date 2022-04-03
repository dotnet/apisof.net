using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;

namespace ApisOfDotNet.Pages;

public partial class CatalogRoot
{
    [Inject]
    public CatalogService CatalogService { get; set; }
}