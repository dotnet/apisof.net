using ApiCatalog.CatalogModel;
using ApiCatalog.Services;
using Microsoft.AspNetCore.Components;

namespace ApiCatalog.Pages
{
    public partial class Index
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        public ApiCatalogStatistics Statistics => CatalogService.CatalogStatistics;
    }
}
