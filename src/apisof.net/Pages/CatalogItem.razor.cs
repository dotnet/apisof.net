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

    [Inject]
    public DocumentationResolverService DocumentationResolver { get; set; }

    [Parameter]
    public string Guid { get; set; }

    public string SelectedFramework { get; set; }

    public ApiModel Api { get; set; }

    public ExtensionMethodModel? ExtensionMethod { get; set; }

    public IEnumerable<ApiModel> Breadcrumbs { get; set; }

    public ApiModel Parent { get; set; }

    public ApiAvailability Availability { get; set; }

    public ApiFrameworkAvailability SelectedAvailability { get; set; }

    public PlatformAnnotationContext PlatformAnnotationContext { get; set; }

    public PreviewDescription? SelectedPreviewDescription { get; set; }

    public Markup SelectedMarkup { get; set; }

    public string HelpUrl { get; set; }

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

        var g = System.Guid.Parse(Guid);

        try
        {
            Api = CatalogService.Catalog.GetApiByGuid(g);
            ExtensionMethod = null;
        }
        catch (KeyNotFoundException)
        {
            ExtensionMethod = CatalogService.Catalog.GetExtensionMethodByGuid(g);
            Api = ExtensionMethod.Value.ExtensionMethod;
        }

        Availability = CatalogService.AvailabilityContext.GetAvailability(Api);
        SelectedFramework = SelectFramework(Availability, NavigationManager.GetQueryParameter("fx"));
        SelectedAvailability = Availability.Frameworks.FirstOrDefault(fx => fx.Framework.GetShortFolderName() == SelectedFramework) ??
                               Availability.Frameworks.FirstOrDefault();
        PlatformAnnotationContext = PlatformAnnotationContext.Create(CatalogService.AvailabilityContext, SelectedFramework);
        SelectedPreviewDescription = SelectedAvailability is null ? null : PreviewDescription.Create(Api);

        if (ExtensionMethod is not null)
            Breadcrumbs = ExtensionMethod.Value.ExtendedType.AncestorsAndSelf().Reverse().Append(ExtensionMethod.Value.ExtensionMethod);
        else
            Breadcrumbs = Api.AncestorsAndSelf().Reverse();

        if (ExtensionMethod is not null)
        {
            Parent = ExtensionMethod.Value.ExtendedType;
        }
        else
        {
            if (Api.Kind.IsMember() && Api.Parent is not null)
            {
                Parent = Api.Parent.Value;
            }
            else
            {
                Parent = Api;
            }
        }

        SelectedMarkup = SelectedAvailability?.Declaration.GetMarkup();

        HelpUrl = await DocumentationResolver.ResolveAsync(Api);
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