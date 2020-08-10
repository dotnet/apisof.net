using System.Threading.Tasks;

using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;

namespace ApiCatalogWeb.Pages
{
    public partial class Index
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        public CatalogStats CatalogStats { get; set; }

        protected override async Task OnInitializedAsync()
        {
            CatalogStats = await CatalogService.GetCatalogStatsAsync();
        }
    }
}
