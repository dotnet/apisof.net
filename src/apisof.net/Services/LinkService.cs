using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public sealed class LinkService
{
    private readonly CatalogService _catalogService;
    private readonly NavigationManager _navigationManager;

    public LinkService(CatalogService catalogService, NavigationManager navigationManager)
    {
        _catalogService = catalogService;
        _navigationManager = navigationManager;
    }

    public string ForCatalog(bool useBlankQuery = false)
    {
        var query = useBlankQuery ? "" : GetQuery();
        return $"/catalog{query}";
    }

    public string ForCatalog(NuGetFramework left, NuGetFramework right)
    {
        ThrowIfNull(left);
        ThrowIfNull(right);

        var query = new BrowsingQuery(new DiffParameter(left, right), null);
        return $"/catalog{query}";
    }

    public object ForDiff()
    {
        var query = GetQuery();
        return $"/diff{query}";
    }

    public string ForDiffDownload(NuGetFramework left, NuGetFramework right)
    {
        ThrowIfNull(left);
        ThrowIfNull(right);

        var query = new BrowsingQuery(new DiffParameter(left, right), null);
        return $"/catalog/download/diff{query}";
    }

    public string For(ApiModel api, NuGetFramework? selected = null)
    {
        ThrowIfDefault(api);

        return ForApiOrExtensionMethod(api.Guid, selected);
    }

    public string For(ApiModel api, NuGetFramework leftFramework, NuGetFramework rightFramework, NuGetFramework? selected = null)
    {
        ThrowIfDefault(api);
        ThrowIfNull(leftFramework);
        ThrowIfNull(rightFramework);

        return ForApiOrExtensionMethod(api.Guid, leftFramework, rightFramework, selected);
    }

    public string For(ExtensionMethodModel extensionMethod, NuGetFramework? selected = null)
    {
        ThrowIfDefault(extensionMethod);

        return ForApiOrExtensionMethod(extensionMethod.Guid, selected);
    }

    public string For(ExtensionMethodModel extensionMethod, NuGetFramework leftFramework, NuGetFramework rightFramework, NuGetFramework? selected = null)
    {
        ThrowIfDefault(extensionMethod);
        ThrowIfNull(leftFramework);
        ThrowIfNull(rightFramework);

        return ForApiOrExtensionMethod(extensionMethod.Guid, leftFramework, rightFramework, selected);
    }

    public string ForApiOrExtensionMethod(Guid guid, NuGetFramework? selected = null)
    {
        var query = GetQuery(selected);
        return $"/catalog/{guid:N}{query}";
    }

    private string ForApiOrExtensionMethod(Guid guid, NuGetFramework leftFramework, NuGetFramework rightFramework, NuGetFramework? selected = null)
    {
        var query = GetQuery(leftFramework, rightFramework, selected);
        return $"/catalog/{guid:N}{query}";
    }

    private string GetQuery(NuGetFramework? selected = null)
    {
        var query = BrowsingQuery.Get(_catalogService.AvailabilityContext, _navigationManager);

        if (selected is not null)
            query = query with { Fx = new FxParameter(selected) };

        return query.ToString();
    }

    private string GetQuery(NuGetFramework leftFramework, NuGetFramework rightFramework, NuGetFramework? selected = null)
    {
        var newDiff = leftFramework != rightFramework
            ? new DiffParameter(leftFramework, rightFramework)
            : (DiffParameter?)null;

        var query = BrowsingQuery.Get(_catalogService.AvailabilityContext, _navigationManager) with
        {
            Diff = newDiff
        };

        if (selected is not null)
            query = query with { Fx = new FxParameter(selected) };

        return query.ToString();
    }
}
