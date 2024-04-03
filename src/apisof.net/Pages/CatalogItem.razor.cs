using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class CatalogItem
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    [Parameter]
    public string? GuidText { get; set; }

    public ApiView? ApiView { get; set; }

    public ApiModel? Api { get; set; }

    public ExtensionMethodModel? ExtensionMethod { get; set; }

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
        ApiView = null;
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
            var framework = QueryManager.GetQueryParameter("fx");
            var apiAvailability = CatalogService.AvailabilityContext.GetAvailability(Api.Value);
            framework = SelectFramework(apiAvailability, framework);
            ApiView = new ApiView(CatalogService.AvailabilityContext, framework);
        }
    }

    private static string SelectFramework(ApiAvailability availability, string? selectedFramework)
    {
        var result = selectedFramework;
        
        // Let's first reset the selected framework in case the current API doesn't support it

        if (!availability.Frameworks.Any(fx => string.Equals(fx.Framework.GetShortFolderName(), result, StringComparison.OrdinalIgnoreCase)))
            result = null;

        // First we try to pick the highest .NET Core framework

        result ??= availability.Frameworks.Where(fx => fx.Framework.Framework == ".NETCoreApp")
            .OrderByDescending(fx => fx.Framework.Version)
            .ThenBy(fx => fx.Framework.HasPlatform)
            .Select(fx => fx.Framework.GetShortFolderName())
            .FirstOrDefault();

        // If we couldn't find any, pick the highest version of any framework

        result ??= availability.Frameworks.OrderBy(f => f.Framework.Framework)
            .ThenByDescending(f => f.Framework.Version)
            .Select(fx => fx.Framework.GetShortFolderName())
            .First();

        return result;
    }
    
    private void QueryManagerOnQueryChanged(object? sender, IReadOnlySet<string> e)
    {
        UpdateApi();
        StateHasChanged();
    }
}