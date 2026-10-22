using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public static class DiffExtensions
{
    public static DiffKind? GetDiffKind(this ApiModel api,
                                        NuGetFramework left,
                                        NuGetFramework right)
    {
        ThrowIfDefault(api);
        ThrowIfNull(left);
        ThrowIfNull(right);

        var defLeft = api.GetDefinition(left);
        var defRight = api.GetDefinition(right);

        if (defLeft is ApiDeclarationModel leftDeclaration && !TryGetMarkupId(leftDeclaration, out _))
            defLeft = null;

        if (defRight is ApiDeclarationModel rightDeclaration && !TryGetMarkupId(rightDeclaration, out _))
            defRight = null;

        if (defLeft is null && defRight is null)
            return null;

        if (defLeft is null)
            return DiffKind.Added;

        if (defRight is null)
            return DiffKind.Removed;

        TryGetMarkupId(defLeft.Value, out var leftMarkupId);
        TryGetMarkupId(defRight.Value, out var rightMarkupId);
        return leftMarkupId == rightMarkupId ? DiffKind.None : DiffKind.Changed;
    }

    private static bool TryGetMarkupId(ApiDeclarationModel declaration, out int markupId)
    {
        try
        {
            markupId = declaration.MarkupId;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            markupId = default;
            return false;
        }
    }

    public static void GetDiffCount(this ApiModel api,
                                    NuGetFramework left,
                                    NuGetFramework right,
                                    ref int added,
                                    ref int removed,
                                    ref int modified)
    {
        ThrowIfDefault(api);
        ThrowIfNull(left);
        ThrowIfNull(right);

        if (!api.CanHaveChildren())
            return;

        foreach (var child in api.Children)
        {
            if (child.Kind.IsAccessor())
                continue;

            var diffKind = child.GetDiffKind(left, right);
            if (diffKind is not null)
            {
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
            }

            child.GetDiffCount(left, right, ref added, ref removed, ref modified);
        }
    }

    public static bool ContainsDifferences(this ApiModel api,
                                           NuGetFramework left,
                                           NuGetFramework right,
                                           DiffOptions diffOptions)
    {
        ThrowIfDefault(api);
        ThrowIfNull(left);
        ThrowIfNull(right);

        var diffKind = api.GetDiffKind(left, right);
        var hasRelevantDifference = diffKind?.IsIncluded(diffOptions) == true;

        if (hasRelevantDifference)
            return true;

        if (CanHaveChildren(api))
        {
            foreach (var child in api.Children)
            {
                if (child.Kind.IsAccessor())
                    continue;

                if (child.ContainsDifferences(left, right, diffOptions))
                    return true;
            }
        }

        return false;
    }

    public static bool IsIncluded(this DiffKind kind, DiffOptions options)
    {
        return kind == DiffKind.None && options.HasFlag(DiffOptions.IncludeUnchanged) ||
               kind == DiffKind.Added && options.HasFlag(DiffOptions.IncludeAdded) ||
               kind == DiffKind.Removed && options.HasFlag(DiffOptions.IncludeRemoved) ||
               kind == DiffKind.Changed && options.HasFlag(DiffOptions.IncludeChanged);
    }

    public static bool CanHaveChildren(this ApiModel api)
    {
        ThrowIfDefault(api);

        return api.Kind == ApiKind.Namespace ||
               api.Kind.IsType() && api.Kind != ApiKind.Delegate;
    }
}