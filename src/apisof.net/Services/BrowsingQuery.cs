using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public readonly record struct BrowsingQuery(DiffParameter? Diff, FxParameter? Fx)
{
    public static BrowsingQuery Get(ApiAvailabilityContext context, NavigationManager navigationManager)
    {
        ThrowIfNull(context);
        ThrowIfNull(navigationManager);

        var diff = DiffParameter.Get(context, navigationManager);
        var fx = FxParameter.Get(context, navigationManager);
        return new BrowsingQuery(diff, fx);
    }

    public override string ToString()
    {
        return (Diff, Fx) switch {
            (not null, not null) => $"?{Diff}&{Fx}",
            (not null, null) => $"?{Diff}",
            (null, not null) => $"?{Fx}",
            _ => ""
        };
    }
}

public readonly record struct FxParameter(NuGetFramework Framework)
{
    public static FxParameter? Get(ApiAvailabilityContext context, NavigationManager navigationManager)
    {
        ThrowIfNull(context);
        ThrowIfNull(navigationManager);

        var fx = navigationManager.GetQueryParameter("fx");
        if (fx is null)
            return null;

        var framework = NuGetFramework.Parse(fx);
        if (!context.IsKnownFramework(framework))
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
    public static DiffParameter? Get(ApiAvailabilityContext context, NavigationManager navigationManager)
    {
        ThrowIfNull(context);
        ThrowIfNull(navigationManager);

        var diff = navigationManager.GetQueryParameter("diff");
        return Parse(context, diff);
    }

    public static DiffParameter? Parse(ApiAvailabilityContext context, string? diff)
    {
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

        if (!context.IsKnownFramework(left) ||
            !context.IsKnownFramework(right))
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