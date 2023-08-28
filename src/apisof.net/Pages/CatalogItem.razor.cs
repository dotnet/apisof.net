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

    public PlatformAnnotationContext PlatformAnnotationContext { get; set; }

    public PreviewRequirementModel? PreviewRequirement { get; set; }

    public ExperimentalModel? Experimental { get; set; }

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
        // TODO: Handle invalid GUID
        // TODO: Handle API not found

        Api = CatalogService.Catalog.GetApiByGuid(System.Guid.Parse(Guid));
        Availability = CatalogService.AvailabilityContext.GetAvailability(Api);
        SelectedFramework = SelectFramework(Availability, NavigationManager.GetQueryParameter("fx"));
        SelectedAvailability = Availability.Frameworks.FirstOrDefault(fx => fx.Framework.GetShortFolderName() == SelectedFramework) ??
                               Availability.Frameworks.FirstOrDefault();
        PlatformAnnotationContext = PlatformAnnotationContext.Create(CatalogService.AvailabilityContext, SelectedFramework);
        PreviewRequirement = GetPreviewRequirement();
        Experimental = GetExperimental();

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

    private static string SelectFramework(ApiAvailability availability, string selectedFramework)
    {
        // Let's first reset the selected framework in case the current API doesn't support it

        if (!availability.Frameworks.Any(fx => string.Equals(fx.Framework.GetShortFolderName(), selectedFramework, StringComparison.OrdinalIgnoreCase)))
            selectedFramework = null;

        // First we try to pick the highest .NET Core framework

        selectedFramework ??= availability.Frameworks.Where(fx => fx.Framework.Framework == ".NETCoreApp")
                                                     .OrderByDescending(fx => fx.Framework.Version)
                                                     .ThenBy(fx => fx.Framework.HasPlatform)
                                                     .Select(fx => fx.Framework.GetShortFolderName())
                                                     .FirstOrDefault();

        // If we couldn't find any, pick the highest version of any framework

        selectedFramework ??= availability.Frameworks.OrderBy(f => f.Framework.Framework)
                                                     .ThenByDescending(f => f.Framework.Version)
                                                     .Select(fx => fx.Framework.GetShortFolderName())
                                                     .FirstOrDefault();

        return selectedFramework;
    }

    private PreviewRequirementModel? GetPreviewRequirement()
    {
        if (SelectedAvailability is null)
            return null;

        var assembly = SelectedAvailability.Declaration.Assembly;

        foreach (var api in Api.AncestorsAndSelf())
        {
            if (api.Kind == ApiKind.Namespace)
                break;

            var declaration = api.Declarations.First(d => d.Assembly == assembly);
            if (declaration.PreviewRequirement is not null)
                return declaration.PreviewRequirement;
        }

        return assembly.PreviewRequirement;
    }

    private ExperimentalModel? GetExperimental()
    {
        if (SelectedAvailability is null)
            return null;

        var assembly = SelectedAvailability.Declaration.Assembly;

        foreach (var api in Api.AncestorsAndSelf())
        {
            if (api.Kind == ApiKind.Namespace)
                break;

            var declaration = api.Declarations.First(d => d.Assembly == assembly);
            if (declaration.Experimental is not null)
                return declaration.Experimental;
        }

        return assembly.Experimental;
    }

    private async void NavigationManager_LocationChanged(object sender, LocationChangedEventArgs e)
    {
        await UpdateSyntaxAsync();
        StateHasChanged();
    }
}