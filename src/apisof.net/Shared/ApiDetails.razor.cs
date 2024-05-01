using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using NuGet.Frameworks;

using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.DesignNotes;
using Terrajobst.ApiCatalog.Features;

namespace ApisOfDotNet.Shared;

public partial class ApiDetails
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required SourceResolverService SourceResolver { get; set; }

    [Inject]
    public required DocumentationResolverService DocumentationResolver { get; set; }

    [Inject]
    public required LinkService Link { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public ApiModel Api { get; set; }

    [Parameter]
    public ExtensionMethodModel? ExtensionMethod { get; set; }

    [Parameter]
    public required ApiBrowsingContext BrowsingContext { get; set; }

    public required IEnumerable<ApiModel> Breadcrumbs { get; set; }

    public required ApiModel Parent { get; set; }

    public required IReadOnlyList<(FeatureUsageSource Source, IReadOnlyList<(FeatureDefinition Feature, float Percentage)> Usages)> Usages { get; set; }

    public required ApiAvailability Availability { get; set; }

    public required ApiFrameworkAvailability? SelectedAvailability { get; set; }

    public required PlatformAnnotationContext PlatformAnnotationContext { get; set; }

    public required PreviewDescription? SelectedPreviewDescription { get; set; }

    private string? SourceUrl { get; set; }

    private string? HelpUrl { get; set; }

    private ImmutableArray<DesignNote> DesignReviews { get; set; } = ImmutableArray<DesignNote>.Empty;

    protected override async Task OnParametersSetAsync()
    {
        await UpdateApiAsync();
    }

    private async Task UpdateApiAsync()
    {
        Usages = GetUsages();
        Availability = Api.GetAvailability();
        SelectedAvailability = Availability.Frameworks.FirstOrDefault(fx => fx.Framework == BrowsingContext.SelectedFramework) ??
                               Availability.Frameworks
                                    .OrderByDescending(fx => fx.Framework.Version)
                                    .ThenBy(fx => fx.Framework.Framework)
                                    .ThenBy(fx => fx.Framework.HasPlatform)
                                    .OrderByDescending(fx => fx.Framework.PlatformVersion)
                                    .First();
        PlatformAnnotationContext = PlatformAnnotationContext.Create(CatalogService.Catalog, SelectedAvailability.Framework.GetShortFolderName());
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

        DesignReviews = CatalogService.DesignNoteDatabase.GetDesignNotes(Api.Guid);

        var results = await Task.WhenAll(
            SourceResolver.ResolveAsync(Api),
            DocumentationResolver.ResolveAsync(Api)
        );

        SourceUrl = results[0];
        HelpUrl = results[1];
    }

    private IEnumerable<ApiFrameworkAvailability> GetSelectedPlatforms()
    {
        if (SelectedAvailability is null)
            return Enumerable.Empty<ApiFrameworkAvailability>();

        var framework = SelectedAvailability.Framework.Framework;
        var frameworkVersion = SelectedAvailability.Framework.Version;

        return Availability.Frameworks.Where(fx => string.Equals(fx.Framework.Framework, framework, StringComparison.OrdinalIgnoreCase) &&
                                                   fx.Framework.Version == frameworkVersion &&
                                                   fx.Framework.HasPlatform &&
                                                   fx.Framework.PlatformVersion != FrameworkConstants.EmptyVersion);
    }

    private ApiFrameworkAvailability? GetCrossPlatformAvailability()
    {
        if (SelectedAvailability is null)
            return null;

        var framework = SelectedAvailability.Framework.Framework;
        var frameworkVersion = SelectedAvailability.Framework.Version;

        return Availability.Frameworks.FirstOrDefault(fx => string.Equals(fx.Framework.Framework, framework, StringComparison.OrdinalIgnoreCase) &&
                                                      fx.Framework.Version == frameworkVersion &&
                                                      fx.Framework.IsPlatformNeutral());
    }

    private IReadOnlyList<(FeatureUsageSource Source, IReadOnlyList<(FeatureDefinition Feature, float Percentage)> Usages)> GetUsages()
    {
        var usages = new List<(FeatureUsageSource Source, FeatureDefinition Feature, float Percentage)>();
        var usageData = CatalogService.UsageData;
        foreach (var feature in FeatureDefinition.ApiFeatures)
        {
            var featureId = feature.GetFeatureId(Api.Guid);
            foreach (var (usageSource, percentage) in usageData.GetUsage(featureId))
                usages.Add((usageSource, feature, percentage));
        }

        if (Api.Kind == ApiKind.Property)
        {
            var getter = Api.Children.Where(a => a.Kind == ApiKind.PropertyGetter).Cast<ApiModel?>().FirstOrDefault();
            var setter = Api.Children.Where(a => a.Kind == ApiKind.PropertySetter).Cast<ApiModel?>().FirstOrDefault();

            // Let's only split usage into get/set when both are present.
            if (getter is not null && setter is not null)
            {
                var getterFeatureId = FeatureDefinition.ReferencesApi.GetFeatureId(getter.Value.Guid);
                foreach (var (usageSource, percentage) in usageData.GetUsage(getterFeatureId))
                    usages.Add((usageSource, FeatureDefinition.GetProperty, percentage));

                var setterFeatureId = FeatureDefinition.ReferencesApi.GetFeatureId(setter.Value.Guid);
                foreach (var (usageSource, percentage) in usageData.GetUsage(setterFeatureId))
                    usages.Add((usageSource, FeatureDefinition.SetProperty, percentage));
            }
        }

        if (Api.Kind == ApiKind.Event)
        {
            var adder = Api.Children.Where(a => a.Kind == ApiKind.EventAdder).Cast<ApiModel?>().FirstOrDefault();
            var remover = Api.Children.Where(a => a.Kind == ApiKind.EventRemover).Cast<ApiModel?>().FirstOrDefault();

            if (adder is not null)
            {
                var adderFeatureId = FeatureDefinition.ReferencesApi.GetFeatureId(adder.Value.Guid);
                foreach (var (usageSource, percentage) in usageData.GetUsage(adderFeatureId))
                    usages.Add((usageSource, FeatureDefinition.AddEvent, percentage));
            }

            if (remover is not null)
            {
                var removerFeatureId = FeatureDefinition.ReferencesApi.GetFeatureId(remover.Value.Guid);
                foreach (var (usageSource, percentage) in usageData.GetUsage(removerFeatureId))
                    usages.Add((usageSource, FeatureDefinition.RemoveEvent, percentage));
            }
        }

        return usages.GroupBy(u => u.Source)
                     .Select(g => (g.Key, (IReadOnlyList<(FeatureDefinition, float)>)g.Select(t => (t.Feature, t.Percentage)).ToArray()))
                     .ToArray();
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

    private bool IsSelectedFramework(NuGetFramework framework, bool matchAnyPlatform = true)
    {
        return BrowsingContext.SelectedFramework == framework ||
               matchAnyPlatform && framework.IsPlatformNeutral() && framework == BrowsingContext.SelectedFramework?.GetBaseFrameworkOrSelf();
    }

    private bool IsLeftFramework(NuGetFramework framework)
    {
        return BrowsingContext is FrameworkDiffBrowsingContext diff && diff.Left == framework;
    }

    private bool IsRightFramework(NuGetFramework framework)
    {
        return BrowsingContext is FrameworkDiffBrowsingContext diff && diff.Right == framework;
    }

    private void VersionClick(MouseEventArgs e, NuGetFramework framework)
    {
        NuGetFramework? GetSelectedFramework()
        {
            return BrowsingContext.SelectedFramework;
        }

        NuGetFramework? GetLeftFramework()
        {
            return BrowsingContext switch {
                FrameworkBrowsingContext frameworkBrowsingContext => frameworkBrowsingContext.Framework,
                FrameworkDiffBrowsingContext diffBrowsingContext => diffBrowsingContext.Left,
                _ => null
            };
        }

        NuGetFramework? GetRightFramework()
        {
            return BrowsingContext switch {
                FrameworkBrowsingContext frameworkBrowsingContext => frameworkBrowsingContext.Framework,
                FrameworkDiffBrowsingContext diffBrowsingContext => diffBrowsingContext.Right,
                _ => null
            };
        }

        var setSelected = e is { CtrlKey: false, AltKey: false };
        var setDiffLeft = e.CtrlKey;
        var setDiffRight = e.AltKey;

        var selected = setSelected
            ? framework
            : GetSelectedFramework();

        var left = setDiffLeft
                    ? framework
                    : GetLeftFramework();

        var right = setDiffRight
                    ? framework
                    : GetRightFramework();

        if (left is not null && right is not null)
        {
            var link = ExtensionMethod is not null
                ? Link.For(ExtensionMethod.Value, left, right, selected)
                : Link.For(Api, left, right, selected);
            NavigationManager.NavigateTo(link);
        }
        else
        {
            var link = ExtensionMethod is not null
                ? Link.For(ExtensionMethod.Value, selected)
                : Link.For(Api, selected);
            NavigationManager.NavigateTo(link);
        }
    }
}