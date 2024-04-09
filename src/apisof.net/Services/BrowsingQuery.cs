using System.Text;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public readonly record struct BrowsingQuery(DiffParameter? Diff, ExcludeUnchangedParameter? ExcludeUnchanged, FxParameter? Fx)
{
    public static BrowsingQuery Get(ApiCatalogModel catalog, NavigationManager navigationManager)
    {
        ThrowIfNull(catalog);
        ThrowIfNull(navigationManager);

        var diff = DiffParameter.Get(catalog, navigationManager);
        var excludeUnchanged = ExcludeUnchangedParameter.Get(navigationManager);
        var fx = FxParameter.Get(catalog, navigationManager);
        return new BrowsingQuery(diff, excludeUnchanged, fx);
    }

    public override string ToString()
    {
        var builder = new QueryBuilder();
        builder.Add(Diff);
        builder.Add(ExcludeUnchanged);
        builder.Add(Fx);
        return builder.ToString();
    }

    private readonly struct QueryBuilder()
    {
        private readonly StringBuilder _sb = new();

        public void Add<T>(T? value)
            where T: struct
        {
            if (value is null)
                return;

            _sb.Append(_sb.Length == 0 ? '?' : '&');
            _sb.Append(value);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}

public readonly record struct FxParameter(NuGetFramework Framework)
{
    public static FxParameter? Get(ApiCatalogModel catalog, NavigationManager navigationManager)
    {
        ThrowIfNull(catalog);
        ThrowIfNull(navigationManager);

        var fx = navigationManager.GetQueryParameter("fx");
        if (fx is null)
            return null;

        var framework = NuGetFramework.Parse(fx);
        if (catalog.GetFramework(framework) is null)
            return null;

        return new FxParameter(framework);
    }

    public override string ToString()
    {
        return $"fx={Framework.GetShortFolderName()}";
    }
}

public readonly record struct DiffParameter(NuGetFramework Left, NuGetFramework Right)
{
    public static DiffParameter? Get(ApiCatalogModel catalog, NavigationManager navigationManager)
    {
        ThrowIfNull(catalog);
        ThrowIfNull(navigationManager);

        var diff = navigationManager.GetQueryParameter("diff");
        return Parse(catalog, diff);
    }

    public static DiffParameter? Parse(ApiCatalogModel catalog, string? diff)
    {
        ThrowIfNull(catalog);

        if (string.IsNullOrEmpty(diff))
            return null;

        const string separator = "-vs-";
        var indexOfSeparator = diff.IndexOf(separator, StringComparison.Ordinal);
        if (indexOfSeparator < 0)
            return null;

        var l = diff.Substring(0, indexOfSeparator).Trim();
        var r = diff.Substring(indexOfSeparator + separator.Length).Trim();

        if (l.Length == 0 || r.Length == 0)
            return null;

        var left = NuGetFramework.Parse(l);
        var right = NuGetFramework.Parse(r);

        if (catalog.GetFramework(left) is null ||
            catalog.GetFramework(right) is null)
            return null;

        return new DiffParameter(left, right);
    }

    public override string ToString()
    {
        var l = Left.GetShortFolderName();
        var r = Right.GetShortFolderName();
        return $"diff={l}-vs-{r}";
    }
}

public readonly record struct ExcludeUnchangedParameter
{
    public static ExcludeUnchangedParameter? Get(NavigationManager navigationManager)
    {
        ThrowIfNull(navigationManager);

        var valueText = navigationManager.GetQueryParameter("no-unchanged");
        var value = bool.TryParse(valueText, out var b) && b;
        return Get(value);
    }

    public static ExcludeUnchangedParameter? Get(bool value)
    {
        return value ? new ExcludeUnchangedParameter() : null;
    }

    public override string ToString()
    {
        return "no-unchanged=true";
    }
}

public readonly record struct ReturnUrlParameter(string ReturnUrl)
{
    public static ReturnUrlParameter? Get(NavigationManager navigationManager)
    {
        ThrowIfNull(navigationManager);

        var returnUrl = navigationManager.GetQueryParameter("returnUrl");
        return string.IsNullOrEmpty(returnUrl)
            ? null
            : new ReturnUrlParameter(returnUrl);
    }
}