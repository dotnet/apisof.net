using System.Text;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public abstract class ApiBrowsingContext
{
    public static ApiBrowsingContext Empty { get; } = new EmptyApiBrowsingContext();

    public static ApiBrowsingContext ForFramework(NuGetFramework? selected)
    {
        if (selected is null)
            return Empty;

        return new FrameworkBrowsingContext(selected);
    }

    public static FrameworkDiffBrowsingContext ForFrameworkDiff(ApiCatalogModel catalog, NuGetFramework left, NuGetFramework right, NuGetFramework? selected)
    {
        return new FrameworkDiffBrowsingContext(catalog, left, right, selected);
    }

    public virtual NuGetFramework? SelectedFramework => null;

    public virtual ApiBrowsingData? GetData(ApiModel api)
    {
        return null;
    }

    public ApiBrowsingData? GetData(ExtensionMethodModel api)
    {
        return GetData(api.ExtensionMethod);
    }

    private sealed class EmptyApiBrowsingContext : ApiBrowsingContext
    {
    }
}

public readonly struct ApiBrowsingData
{
    public string? CssClasses { get; init; }
    public MarkupString? AdditionalMarkup { get; init; }
    public bool Excluded { get; init; }
}

public sealed class FrameworkBrowsingContext : ApiBrowsingContext
{
    private readonly NuGetFramework _framework;

    public FrameworkBrowsingContext(NuGetFramework framework)
    {
        _framework = framework;
    }

    public override NuGetFramework? SelectedFramework => _framework;

    public NuGetFramework Framework
    {
        get { return _framework; }
    }

    public override ApiBrowsingData? GetData(ApiModel api)
    {
        var cssClasses = api.GetDefinition(_framework) is null ? "text-muted" : null;
        return new ApiBrowsingData { CssClasses = cssClasses };
    }
}

public sealed class FrameworkDiffBrowsingContext : ApiBrowsingContext
{
    private readonly ApiCatalogModel _catalog;
    private readonly NuGetFramework _left;
    private readonly NuGetFramework _right;
    private readonly NuGetFramework? _selectedFramework;

    public FrameworkDiffBrowsingContext(ApiCatalogModel catalog, NuGetFramework left, NuGetFramework right, NuGetFramework? selectedFramework)
    {
        _catalog = catalog;
        _left = left;
        _right = right;
        _selectedFramework = selectedFramework;
    }

    public override NuGetFramework? SelectedFramework
    {
        get { return _selectedFramework; }
    }

    public NuGetFramework Left
    {
        get { return _left; }
    }

    public NuGetFramework Right
    {
        get { return _right; }
    }

    public override ApiBrowsingData? GetData(ApiModel api)
    {
        var diffKind = api.GetDiffKind(_left, _right);
        if (diffKind is null)
            return new ApiBrowsingData { Excluded = true };

        if (diffKind == DiffKind.Added)
            return new ApiBrowsingData { CssClasses = "diff-added" };

        if (diffKind == DiffKind.Removed)
            return new ApiBrowsingData { CssClasses = "diff-removed" };

        var cssClasses = "";

        if (diffKind == DiffKind.Changed)
            cssClasses = "diff-changed";

        MarkupString? additionalMarkup = null;

        var added = 0;
        var removed = 0;
        var modified = 0;
        api.GetDiffCount(_left, _right, ref added, ref removed, ref modified);

        var anyChanges = added + removed + modified > 0;
        if (anyChanges)
        {
            var sb = new StringBuilder();
            sb.Append("""<span class="diff-details">""");

            if (added > 0)
                sb.Append($"+{added:N0}");

            if (removed > 0)
            {
                if (added > 0)
                    sb.Append(' ');

                sb.Append($"-{removed:N0}");
            }

            if (modified > 0)
            {
                if (added + removed > 0)
                    sb.Append(' ');

                sb.Append($"~{modified:N0}");
            }

            sb.Append("</span>");

            additionalMarkup = new MarkupString(sb.ToString());
        }

        return new ApiBrowsingData {
            CssClasses = cssClasses,
            AdditionalMarkup = additionalMarkup
        };
    }
}
