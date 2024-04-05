using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class CatalogItem
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string? GuidText { get; set; }

    public ApiModel? Api { get; set; }

    public ExtensionMethodModel? ExtensionMethod { get; set; }

    public ApiBrowsingContext BrowsingContext { get; set; } = ApiBrowsingContext.Empty;

    protected override void OnInitialized()
    {
        QueryManager.QueryChanged += QueryManagerOnQueryChanged;
    }

    protected override void OnParametersSet()
    {
        UpdateApi();
    }

    private void UpdateApi()
    {
        Api = null;
        ExtensionMethod = null;

        if (!Guid.TryParse(GuidText, out var guid))
            return;

        try
        {
            Api = CatalogService.Catalog.GetApiByGuid(guid);
            ExtensionMethod = null;
        }
        catch (KeyNotFoundException)
        {
            try
            {
                ExtensionMethod = CatalogService.Catalog.GetExtensionMethodByGuid(guid);
                Api = ExtensionMethod.Value.ExtensionMethod;
            }
            catch (KeyNotFoundException)
            {
                // Ignore
            }
        }

        if (Api is not null)
        {
            var query = BrowsingQuery.Get(CatalogService.AvailabilityContext, NavigationManager);

            var framework = query.Fx?.Framework;

            if (framework is not null && !CatalogService.AvailabilityContext.IsAvailable(Api.Value, framework))
                framework = null;

            if (query.Diff is null)
            {
                framework ??= SelectFramework(CatalogService.AvailabilityContext, Api.Value);
                BrowsingContext = ApiBrowsingContext.ForFramework(CatalogService.AvailabilityContext, framework);
            }
            else
            {
                var left = query.Diff.Value.Left;
                var right = query.Diff.Value.Right;

                if (framework is null)
                {
                    if (CatalogService.AvailabilityContext.IsAvailable(Api.Value, right))
                        framework = right;
                    else if (CatalogService.AvailabilityContext.IsAvailable(Api.Value, left))
                        framework = left;
                    else
                        framework = SelectFramework(CatalogService.AvailabilityContext, Api.Value);
                }

                BrowsingContext = ApiBrowsingContext.ForFrameworkDiff(CatalogService.AvailabilityContext, left, right, framework);
            }
        }
    }

    private static NuGetFramework SelectFramework(ApiAvailabilityContext context, ApiModel model)
    {
        var availability = context.GetAvailability(model);

        // First we try to pick the highest .NET Core framework

        var result = availability.Frameworks.Where(fx => fx.Framework.Framework == ".NETCoreApp")
                                            .OrderByDescending(fx => fx.Framework.Version)
                                            .ThenBy(fx => fx.Framework.HasPlatform)
                                            .Select(fx => fx.Framework)
                                            .FirstOrDefault();

        // If we couldn't find any, pick the highest version of any framework

        result ??= availability.Frameworks.OrderBy(f => f.Framework.Framework)
                                          .ThenByDescending(f => f.Framework.Version)
                                          .Select(fx => fx.Framework)
                                          .First();

        return result;
    }

    private void QueryManagerOnQueryChanged(object? sender, IReadOnlySet<string> e)
    {
        UpdateApi();
        StateHasChanged();
    }
}