using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ApiCatalogWeb.Pages
{
    public partial class CatalogItem
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public string Guid { get; set; }

        public CatalogApiSpine Spine { get; set; }

        public CatalogAvailability Availability { get; set; }

        public string SelectedFramework { get; set; }

        public string SelectedSyntax { get; set; }

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
        }

        protected override async Task OnParametersSetAsync()
        {
            Spine = await CatalogService.GetSpineAsync(Guid);
            Availability = await CatalogService.GetAvailabilityAsync(Guid, null);
            await UpdateSyntaxAsync();
        }

        private async Task UpdateSyntaxAsync()
        {
            SelectedFramework = NavigationManager.GetQueryParameter("fx");
            Availability = await CatalogService.GetAvailabilityAsync(Guid, SelectedFramework);

            SelectedSyntax = Availability.Current == null
                ? ""
                : await CatalogService.GetSyntaxAsync(Spine.Current.ApiGuid, Availability.Current.AssemblyFingerprint);
        }

        private async void NavigationManager_LocationChanged(object sender, LocationChangedEventArgs e)
        {
            await UpdateSyntaxAsync();
            StateHasChanged();
        }
    }
}
