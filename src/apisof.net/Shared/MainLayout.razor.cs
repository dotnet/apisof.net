using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;

namespace ApisOfDotNet.Shared;

public partial class MainLayout
{
    public required ApiSearch ApiSearch;
    
    [Inject]
    public required CatalogService CatalogService { get; set; }
    
    public CatalogJobInfo CatalogJobInfo => CatalogService.JobInfo;
}