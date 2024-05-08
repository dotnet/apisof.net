﻿using ApisOfDotNet.Services;
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
            var query = BrowsingQuery.Get(CatalogService.Catalog, NavigationManager);

            var framework = query.Fx?.Framework;

            if (framework is not null && !Api.Value.IsAvailable(framework))
                framework = null;

            if (query.Diff is null)
            {
                framework ??= SelectFramework(Api.Value);
                BrowsingContext = ApiBrowsingContext.ForFramework(framework);
            }
            else
            {
                var left = query.Diff.Value.Left;
                var right = query.Diff.Value.Right;
                var diffOptions = query.DiffOptions?.DiffOptions;

                if (framework is null)
                {
                    if (Api.Value.IsAvailable(right))
                        framework = right;
                    else if (Api.Value.IsAvailable(left))
                        framework = left;
                    else
                        framework = SelectFramework(Api.Value);
                }

                BrowsingContext = ApiBrowsingContext.ForFrameworkDiff(left, right, diffOptions, framework);
            }
        }
    }

    private static NuGetFramework SelectFramework(ApiModel model)
    {
        return model.GetAvailability().GetCurrent().Framework;
    }

    private void QueryManagerOnQueryChanged(object? sender, IReadOnlySet<string> e)
    {
        UpdateApi();
        StateHasChanged();
    }
}