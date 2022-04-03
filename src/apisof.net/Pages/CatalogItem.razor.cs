using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using ApiCatalog;
using ApiCatalog.CatalogModel;
using ApiCatalog.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ApiCatalog.Pages
{
    public partial class CatalogItem
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public string Guid { get; set; }

        public string SelectedFramework { get; set; }

        public ApiModel Api { get; set; }

        public IEnumerable<ApiModel> Breadcrumbs { get; set; }

        public ApiModel Parent { get; set; }

        public ApiAvailability Availability { get; set; }

        public ApiFrameworkAvailability SelectedAvailability { get; set; }

        public Markup SelectedMarkup { get; set; }

        public string HelpLink { get; set; }

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
        }

        protected override async Task OnParametersSetAsync()
        {
            await UpdateSyntaxAsync();
        }

        private async Task UpdateSyntaxAsync()
        {
            SelectedFramework = NavigationManager.GetQueryParameter("fx");

            // TODO: Handle invalide GUID
            // TODO: Handle API not found

            Api = CatalogService.GetApiByGuid(System.Guid.Parse(Guid));
            Availability = Api.GetAvailability();
            SelectedAvailability = Availability.Frameworks.FirstOrDefault(fx => fx.Framework.GetShortFolderName() == SelectedFramework);
            if (SelectedAvailability == null)
                SelectedAvailability = Availability.Frameworks.FirstOrDefault();

            Breadcrumbs = Api.AncestorsAndSelf().Reverse();

            if (Api.Kind.IsMember())
            {
                Parent = Api.Parent;
            }
            else
            {
                Parent = Api;
            }

            SelectedMarkup = SelectedAvailability == null
                ? null
                : SelectedAvailability.Declaration.GetMarkup();

            var helpLink = Api.GetHelpLink();
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(helpLink);
            if (response.StatusCode == HttpStatusCode.OK)
                HelpLink = helpLink;
            else
                HelpLink = null;
        }

        private async void NavigationManager_LocationChanged(object sender, LocationChangedEventArgs e)
        {
            await UpdateSyntaxAsync();
            StateHasChanged();
        }
    }
}
