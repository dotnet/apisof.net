using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Xml;
using Microsoft.AspNetCore.Components;

namespace ApiCatalogWeb.Shared
{
    public partial class SyntaxView
    {
        [Parameter]
        public string Syntax { get; set; }

        [Parameter]
        public string CurrentId { get; set; }

        public MarkupString Markup { get; set; }

        protected override void OnParametersSet()
        {
            var currentGuid = Guid.Parse(CurrentId);

            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Auto
            };
            using var stringReader = new StringReader(Syntax);
            using var reader = XmlReader.Create(stringReader, settings);

            var markupBuilder = new StringBuilder();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "p")
                    {
                        markupBuilder.Append("<span class=\"punctuation\">");
                    }
                    else if (reader.LocalName == "k")
                    {
                        markupBuilder.Append("<span class=\"keyword\">");
                    }
                    else if (reader.LocalName == "n")
                    {
                        markupBuilder.Append("<span class=\"number\">");
                    }
                    else if (reader.LocalName == "s")
                    {
                        markupBuilder.Append("<span class=\"string\">");
                    }
                    else if (reader.LocalName == "r")
                    {
                        var id = reader.GetAttribute("i");
                        var hasLink = false;
                        var isCurrent = false;
                        if (id != null)
                        {
                            hasLink = true;
                            var guid = Guid.Parse(id);
                            if (guid == currentGuid)
                                isCurrent = true;
                        }

                        if (isCurrent)
                            markupBuilder.Append("<span class=\"reference-current\">");
                        else
                            markupBuilder.Append("<span class=\"reference\">");

                        if (!isCurrent && hasLink)
                            markupBuilder.Append($"<a href=\"catalog/{id}\">");
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.LocalName == "r")
                        markupBuilder.Append("</a>");

                    markupBuilder.Append("</span>");
                }
                else if (reader.NodeType == XmlNodeType.Text ||
                         reader.NodeType == XmlNodeType.Whitespace ||
                         reader.NodeType == XmlNodeType.SignificantWhitespace)
                {
                    markupBuilder.Append(HtmlEncoder.Default.Encode(reader.Value));
                }
            }


            Markup = new MarkupString(markupBuilder.ToString());
        }
    }
}