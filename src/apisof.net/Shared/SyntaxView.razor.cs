using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public partial class SyntaxView
{
    [Inject]
    public CatalogService CatalogService { get; set; }

    [Inject]
    public IconService IconService { get; set; }

    [Inject]
    public HtmlEncoder HtmlEncoder { get; set; }

    [Parameter]
    public Markup Markup { get; set; }

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

        foreach (var part in Markup.Parts)
        {
            switch (part.Kind)
            {
                case MarkupPartKind.Whitespace:
                    break;
                case MarkupPartKind.LiteralNumber:
                    markupBuilder.Append("<span class=\"number\">");
                    break;
                case MarkupPartKind.LiteralString:
                    markupBuilder.Append("<span class=\"string\">");
                    break;
                case MarkupPartKind.Punctuation:
                    markupBuilder.Append("<span class=\"punctuation\">");
                    break;
                case MarkupPartKind.Keyword:
                    markupBuilder.Append("<span class=\"keyword\">");
                    break;
                case MarkupPartKind.Reference:
                    {
                        var api = part.Reference == null
                            ? (ApiModel?)null
                            : CatalogService.GetApiByGuid(part.Reference.Value);

                        var tooltip = api == null ? null : GeneratedTooltip(api.Value);

                        if (api == Current)
                        {
                            markupBuilder.Append("<span class=\"reference-current\">");
                        }
                        else if (api != null)
                        {
                            var referenceClass = GetReferenceClass(api.Value.Kind);
                            markupBuilder.Append($"<span class=\"{referenceClass}\"");
                            if (tooltip != null)
                                markupBuilder.Append($"data-toggle=\"popover\" data-trigger=\"hover\" data-placement=\"top\" data-html=\"true\" data-content=\"{HtmlEncoder.Default.Encode(tooltip)}\"");

                            markupBuilder.Append(">");
                        }

                        if (api != null && api != Current)
                            markupBuilder.Append($"<a href=\"catalog/{part.Reference:N}\">");

                        break;
                    }
            }

            markupBuilder.Append(HtmlEncoder.Encode(part.Text));

            if (part.Kind == MarkupPartKind.Reference)
                markupBuilder.Append("</a>");

            markupBuilder.Append("</span>");
        }

        return new MarkupString(markupBuilder.ToString());
    }

    private string GeneratedTooltip(ApiModel current)
    {
        var iconUrl = IconService.GetIcon(current.Kind);

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
            case ApiKind.Namespace:
            case ApiKind.Constant:
            case ApiKind.EnumItem:
            case ApiKind.Field:
            case ApiKind.Constructor:
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