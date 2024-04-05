using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Primitives;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public partial class SyntaxView
{
    [Inject] public required CatalogService CatalogService { get; set; }

    [Inject] public required HtmlEncoder HtmlEncoder { get; set; }

    [Inject] public required LinkService Link { get; set; }

    [Parameter] public ApiModel Current { get; set; }

    [Parameter] public required ApiBrowsingContext BrowsingContext { get; set; }

    public required ApiFrameworkAvailability Availability { get; set; }

    public string SyntaxClass { get; set; } = "";

    public MarkupString SyntaxMarkup { get; set; }

    protected override void OnParametersSet()
    {
        UpdateSyntaxClassAndMarkup();
    }

    private void UpdateSyntaxClassAndMarkup()
    {
        var selectedFramework = BrowsingContext.SelectedFramework!;

        IEnumerable<MarkupDiff.MarkupTokenWithDiff> tokenDiff;
        MarkupDiff.DiffKind apiDiff;

        if (BrowsingContext is FrameworkDiffBrowsingContext diff &&
            (selectedFramework == diff.Left || selectedFramework == diff.Right))
        {
            var left = CatalogService.AvailabilityContext.GetDefinition(Current, diff.Left);
            var right = CatalogService.AvailabilityContext.GetDefinition(Current, diff.Right);

            if (left is null)
            {
                Debug.Assert(right is not null);
                apiDiff = MarkupDiff.DiffKind.Added;
                tokenDiff = GetTokensWithoutDiff(right.Value);
                Availability = CatalogService.AvailabilityContext.GetAvailability(Current, diff.Right)!;
            }
            else if (right is null)
            {
                apiDiff = MarkupDiff.DiffKind.Removed;
                tokenDiff = GetTokensWithoutDiff(left.Value);
                Availability = CatalogService.AvailabilityContext.GetAvailability(Current, diff.Left)!;
            }
            else
            {
                apiDiff = MarkupDiff.DiffKind.None;
                tokenDiff = MarkupDiff.Diff(left.Value.GetMarkup(), right.Value.GetMarkup());
                Availability = CatalogService.AvailabilityContext.GetAvailability(Current, diff.Right)!;
            }
        }
        else
        {
            apiDiff = MarkupDiff.DiffKind.None;
            Availability = CatalogService.AvailabilityContext.GetAvailability(Current, selectedFramework)!;
            tokenDiff = GetTokensWithoutDiff(Availability.Declaration);
        }

        Debug.Assert(Availability is not null);

        IEnumerable<MarkupDiff.MarkupTokenWithDiff> GetTokensWithoutDiff(ApiDeclarationModel declaration)
        {
            return declaration
                .GetMarkup()
                .Tokens
                .Select(t => new MarkupDiff.MarkupTokenWithDiff(t, MarkupDiff.DiffKind.None));
        }

        var markupBuilder = new StringBuilder();

        void WriteToken(string text, string cssClass, Guid? link = null, string? tooltip = null)
        {
            markupBuilder.Append($"<span class=\"{cssClass}\"");

            if (tooltip is not null)
                markupBuilder.Append($"data-toggle=\"popover\" data-trigger=\"hover\" data-placement=\"top\" data-html=\"true\" data-content=\"{HtmlEncoder.Default.Encode(tooltip)}\"");

            markupBuilder.Append(">");

            if (link is not null)
                markupBuilder.Append($"<a href=\"{Link.ForApiOrExtensionMethod(link.Value)}\">");

            markupBuilder.Append(HtmlEncoder.Encode(text));

            if (link is not null)
                markupBuilder.Append("</a>");

            markupBuilder.Append("</span>");
        }

        foreach (var (token, tokenDiffKind) in tokenDiff)
        {
            var diffClass = tokenDiffKind switch {
                MarkupDiff.DiffKind.Added => "diff-added",
                MarkupDiff.DiffKind.Removed => "diff-removed",
                _ => null
            };

            if (diffClass is not null)
                markupBuilder.Append($"""<span class="{diffClass}">""");

            switch (token.Kind)
            {
                case MarkupTokenKind.Space:
                case MarkupTokenKind.LineBreak:
                    WriteToken(token.Text, "whitespace");
                    break;
                case MarkupTokenKind.LiteralNumber:
                    WriteToken(token.Text, "number");
                    break;
                case MarkupTokenKind.LiteralString:
                    WriteToken(token.Text, "string");
                    break;
                default:
                {
                    if (token.Kind.IsPunctuation())
                    {
                        WriteToken(token.Text, "punctuation");
                    }
                    else if (token.Kind.IsKeyword())
                    {
                        WriteToken(token.Text, "keyword");
                    }
                    else if (token.Kind == MarkupTokenKind.ReferenceToken)
                    {
                        var api = token.Reference is null
                            ? (ApiModel?)null
                            : CatalogService.Catalog.GetApiByGuid(token.Reference.Value);

                        if (api is null)
                        {
                            WriteToken(token.Text, "reference");
                        }
                        else
                        {
                            var tooltip = GeneratedTooltip(api.Value);
                            var link = api == Current ? (Guid?)null : api.Value.Guid;
                            var cssClass = api == Current ? "reference-current" : GetReferenceClass(api.Value.Kind);
                            WriteToken(token.Text, cssClass, link, tooltip);
                        }
                    }
                    else
                    {
                        throw new Exception($"Unexpected token kind {token.Kind}");
                    }

                    break;
                }
            }

            if (diffClass is not null)
                markupBuilder.Append("</span>");
        }

        SyntaxClass = apiDiff == MarkupDiff.DiffKind.Added
                        ? "diff-added"
                        : apiDiff == MarkupDiff.DiffKind.Removed
                            ? "diff-removed"
                            : "";
        SyntaxMarkup = new MarkupString(markupBuilder.ToString().Trim());
    }

    private string GeneratedTooltip(ApiModel current)
    {
        var iconUrl = current.Kind.GetGlyph().ToUrl();

        var sb = new StringBuilder();
        sb.Append($"<img src=\"{iconUrl}\" heigth=\"16\" width=\"16\" /> ");

        var isFirst = true;

        foreach (var api in current.AncestorsAndSelf().Reverse())
        {
            if (isFirst)
                isFirst = false;
            else
                sb.Append(".");

            sb.Append(HtmlEncoder.Encode(api.Name));
        }

        return sb.ToString();
    }

    private static string GetReferenceClass(ApiKind kind)
    {
        switch (kind)
        {
            case ApiKind.Interface:
            case ApiKind.Delegate:
            case ApiKind.Enum:
            case ApiKind.Struct:
            case ApiKind.Class:
                return kind.ToString().ToLower();
            case ApiKind.Constructor:
                // The only way to see them as a reference is via attributes.
                //
                // When we're rendering constructors themselves, we use a fixed class for the current item.
                return "class";
            case ApiKind.Namespace:
            case ApiKind.Constant:
            case ApiKind.EnumItem:
            case ApiKind.Field:
            case ApiKind.Destructor:
            case ApiKind.Property:
            case ApiKind.PropertyGetter:
            case ApiKind.PropertySetter:
            case ApiKind.Method:
            case ApiKind.Operator:
            case ApiKind.Event:
            case ApiKind.EventAdder:
            case ApiKind.EventRemover:
            case ApiKind.EventRaiser:
            default:
                return "reference";
        }
    }
}