using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using ApiCatalog;

namespace ApiCatalogWeb.Shared
{
    public partial class SyntaxView
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        [Inject]
        public IconService IconService { get; set; }

        [Inject]
        public HtmlEncoder HtmlEncoder { get; set; }

        [Parameter]
        public string Syntax { get; set; }

        [Parameter]
        public string CurrentId { get; set; }

        public MarkupString Output { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            var currentGuid = Guid.Parse(CurrentId);
            var markup = Markup.Parse(Syntax);

            var references = markup.Parts
                                   .Where(p => p.Reference != null)
                                   .Select(p => p.Reference.Value)
                                   .Distinct()
                                   .ToArray();

            var apis = await CatalogService.GetApisWithParentsAsync(references);

            Output = ToMarkupString(currentGuid, apis, markup);
        }

        private MarkupString ToMarkupString(Guid currentGuid, IReadOnlyList<CatalogApi> apis, Markup markup)
        {
            var apiByGuid = apis.ToDictionary(a => Guid.Parse(a.ApiGuid));
            var apiById = apis.ToDictionary(a => a.ApiId);

            var markupBuilder = new StringBuilder();

            foreach (var part in markup.Parts)
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
                        var api = part.Reference == null || !apiByGuid.ContainsKey(part.Reference.Value)
                            ? null
                            : apiByGuid[part.Reference.Value];

                        var isCurrent = part.Reference == currentGuid;
                        var tooltip = api == null ? null : GeneratedTooltip(api, apiById, part.Reference.Value);

                        if (isCurrent)
                        {
                            markupBuilder.Append("<span class=\"reference-current\">");
                        }
                        else if (api != null)
                        {
                            var referenceClass = GetReferenceClass(api.Kind);
                            markupBuilder.Append($"<span class=\"{referenceClass}\"");
                            if (tooltip != null)
                                markupBuilder.Append($"data-toggle=\"popover\" data-trigger=\"hover\" data-placement=\"top\" data-html=\"true\" data-content=\"{HtmlEncoder.Default.Encode(tooltip)}\"");

                            markupBuilder.Append(">");
                        }

                        if (!isCurrent && api != null)
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

        private string GeneratedTooltip(CatalogApi leafApi, Dictionary<int, CatalogApi> apiById, Guid value)
        {
            var apis = new List<CatalogApi>();
            var ancestor = leafApi.ApiId;
            while (ancestor != 0)
            {
                var api = apiById[ancestor];
                apis.Add(api);
                ancestor = api.ParentApiId;
            }

            apis.Reverse();
            var current = apis.Last();

            var iconUrl = IconService.GetIcon(current.Kind);

            var sb = new StringBuilder();
            sb.Append($"<img src=\"{iconUrl}\" heigth=\"16\" width=\"16\" /> ");

            var isFirst = true;
            
            foreach (var api in apis)
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
                case ApiKind.Method:
                case ApiKind.Operator:
                case ApiKind.Event:
                default:
                    return "reference";
            }
        }
    }
}