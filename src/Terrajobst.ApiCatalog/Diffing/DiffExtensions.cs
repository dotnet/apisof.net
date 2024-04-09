using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public static class DiffExtensions
{
    public static DiffKind? GetDiffKind(this ApiModel api,
                                        NuGetFramework left,
                                        NuGetFramework right)
    {
        ThrowIfNull(left);
        ThrowIfNull(right);

        var defLeft = api.GetDefinition(left);
        var defRight = api.GetDefinition(right);

        if (defLeft is null && defRight is null)
            return null;

        if (defLeft is null)
            return DiffKind.Added;

        if (defRight is null)
            return DiffKind.Removed;

        return defLeft.Value.MarkupId == defRight.Value.MarkupId ? DiffKind.None : DiffKind.Changed;
    }

    public static void GetDiffCount(this ApiModel api,
                                    NuGetFramework left,
                                    NuGetFramework right,
                                    ref int added,
                                    ref int removed,
                                    ref int modified)
    {
        ThrowIfNull(left);
        ThrowIfNull(right);

        if (!api.CanHaveChildren())
            return;

        foreach (var child in api.Children)
        {
            if (child.Kind.IsAccessor())
                continue;

            var diffKind = child.GetDiffKind(left, right);
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

            child.GetDiffCount(left, right, ref added, ref removed, ref modified);
        }
    }

    public static bool ContainsDifferences(this ApiModel api,
                                           NuGetFramework left,
                                           NuGetFramework right)
    {
        ThrowIfNull(left);
        ThrowIfNull(right);

        var diffKind = api.GetDiffKind(left, right);
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
                
                if (child.ContainsDifferences(left, right))
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