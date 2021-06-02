using ApiCatalog.CatalogModel;

using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;

namespace ApiCatalogWeb.Pages
{
    public partial class Index
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        public ApiCatalogStatistics Statistics => CatalogService.CatalogStatistics;
    }
}
