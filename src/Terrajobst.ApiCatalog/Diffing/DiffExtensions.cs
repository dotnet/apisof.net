using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public static class DiffExtensions
{
    public static DiffKind? GetDiffKind(this ApiAvailabilityContext context,
                                        NuGetFramework left,
                                        NuGetFramework right,
                                        ApiModel api)
    {
        ThrowIfNull(context);
        ThrowIfNull(left);
        ThrowIfNull(right);

        var defLeft = context.GetDefinition(api, left);
        var defRight = context.GetDefinition(api, right);

        if (defLeft is null && defRight is null)
            return null;

        if (defLeft is null)
            return DiffKind.Added;

        if (defRight is null)
            return DiffKind.Removed;

        return defLeft.Value.MarkupId == defRight.Value.MarkupId ? DiffKind.None : DiffKind.Changed;
    }

    public static void GetDiffCount(this ApiAvailabilityContext context,
                                    NuGetFramework left,
                                    NuGetFramework right,
                                    ApiModel api,
                                    ref int added,
                                    ref int removed,
                                    ref int modified)
    {
        ThrowIfNull(context);
        ThrowIfNull(left);
        ThrowIfNull(right);

        if (!api.CanHaveChildren())
            return;

        foreach (var child in api.Children)
        {
            if (child.Kind.IsAccessor())
                continue;

            var diffKind = context.GetDiffKind(left, right, child);
            if (diffKind is null)
                continue;

            switch (diffKind)
            {
                case DiffKind.Added:
                    added++;
                    break;
                case DiffKind.Removed:
                    removed++;
                    break;
                case DiffKind.Changed:
                    modified++;
                    break;
            }

            GetDiffCount(context, left, right, child, ref added, ref removed, ref modified);
        }
    }

    public static bool ContainsDifferences(this ApiAvailabilityContext context,
                                           NuGetFramework left,
                                           NuGetFramework right,
                                           ApiModel api)
    {
        ThrowIfNull(context);
        ThrowIfNull(left);
        ThrowIfNull(right);

        var diffKind = context.GetDiffKind(left, right, api);
        var hasDifference = diffKind is not null &&
                            diffKind.Value != DiffKind.None;

        if (hasDifference)
            return true;

        if (CanHaveChildren(api))
        {
            foreach (var child in api.Children)
            {
                if (child.Kind.IsAccessor())
                    continue;
                
                if (ContainsDifferences(context, left, right, child))
                    return true;
            }
        }

        return false;
    }

    public static bool CanHaveChildren(this ApiModel api)
    {
        return api.Kind == ApiKind.Namespace ||
               api.Kind.IsType() && api.Kind != ApiKind.Delegate;
    }
}