using ApiCatalog.CatalogModel;
using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;

namespace ApiCatalogWeb.Pages
{
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
}
