#nullable enable

using System.Diagnostics;
using ApisOfDotNet.Pages;
using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public partial class ApiDetails
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required SourceResolverService SourceResolver { get; set; }

    [Inject]
    public required DocumentationResolverService DocumentationResolver { get; set; }

    [Parameter]
    public ApiModel Api { get; set; }

    [Parameter]
    public ExtensionMethodModel? ExtensionMethod { get; set; }

    [Parameter]
    public required ApiView ApiView { get; set; }
    
    public required IEnumerable<ApiModel> Breadcrumbs { get; set; }

    public required ApiModel Parent { get; set; }

    public required ApiAvailability Availability { get; set; }

    public required ApiFrameworkAvailability? SelectedAvailability { get; set; }

    public required PlatformAnnotationContext PlatformAnnotationContext { get; set; }

    public required PreviewDescription? SelectedPreviewDescription { get; set; }

    private Markup? SelectedMarkup { get; set; }

    private string? SourceUrl { get; set; }

    private string? HelpUrl { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await UpdateSyntaxAsync();
    }

    private async Task UpdateSyntaxAsync()
    {
        Availability = CatalogService.AvailabilityContext.GetAvailability(Api);
        SelectedAvailability = Availability.Frameworks.FirstOrDefault(fx => fx.Framework == ApiView.Framework) ??
                               Availability.Frameworks.FirstOrDefault();
        PlatformAnnotationContext = PlatformAnnotationContext.Create(CatalogService.AvailabilityContext, ApiView.Framework.GetShortFolderName());
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

        var results = await Task.WhenAll(
            SourceResolver.ResolveAsync(Api),
            DocumentationResolver.ResolveAsync(Api)
        );
        
        SourceUrl = results[0];
        HelpUrl = results[1];
    }

    private ApiModel? GetAccessor(ApiKind kind)
    {
        Debug.Assert(kind.IsAccessor());

        if (SelectedAvailability is null)
            return null;

        return Api.Children
            .Where(c => c.Kind == kind && c.Declarations.Any(d => d.Assembly == SelectedAvailability.Declaration.Assembly))
            .Cast<ApiModel?>()
            .FirstOrDefault();
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
}