using System.Text;
using System.Text.Encodings.Web;

using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Primitives;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public partial class SyntaxView
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required HtmlEncoder HtmlEncoder { get; set; }

    [Parameter]
    public required Markup Markup { get; set; }

    [Parameter]
    public ApiModel Current { get; set; }

    public MarkupString Output { get; set; }

    protected override void OnParametersSet()
    {
        Output = ToMarkupString();
    }

    private MarkupString ToMarkupString()
    {
        var markupBuilder = new StringBuilder();

        void WriteToken(string text, string cssClass, Guid? link = null, string? tooltip = null)
        {
            markupBuilder.Append($"<span class=\"{cssClass}\"");
            
            if (tooltip is not null)
                markupBuilder.Append($"data-toggle=\"popover\" data-trigger=\"hover\" data-placement=\"top\" data-html=\"true\" data-content=\"{HtmlEncoder.Default.Encode(tooltip)}\"");

            markupBuilder.Append(">");
            
            if (link is not null)
                markupBuilder.Append($"<a href=\"catalog/{link:N}\">");

            markupBuilder.Append(HtmlEncoder.Encode(text));

            if (link is not null)
                markupBuilder.Append("</a>");
            
            markupBuilder.Append("</span>");
        }

        foreach (var token in Markup.Tokens)
        {
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
        }

        return new MarkupString(markupBuilder.ToString().Trim());
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