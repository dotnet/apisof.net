using System.Net;

using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

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

        // TODO: Handle invalid GUID
        // TODO: Handle API not found

        Api = CatalogService.GetApiByGuid(System.Guid.Parse(Guid));
        Availability = Api.GetAvailability();
        SelectedAvailability = Availability.Frameworks.FirstOrDefault(fx => fx.Framework.GetShortFolderName() == SelectedFramework) ??
                               Availability.Frameworks.FirstOrDefault();

        Breadcrumbs = Api.AncestorsAndSelf().Reverse();

        if (Api.Kind.IsMember() && Api.Parent is not null)
        {
            Parent = Api.Parent.Value;
        }
        else
        {
            Parent = Api;
        }

        SelectedMarkup = SelectedAvailability?.Declaration.GetMarkup();

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