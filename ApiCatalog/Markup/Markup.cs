using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml;

namespace ApiCatalog
{
    public class Markup
    {
        public Markup(IEnumerable<MarkupPart> parts)
        {
            Parts = parts.ToImmutableArray();
        }

        public ImmutableArray<MarkupPart> Parts { get; }

        public static Markup Parse(string text)
        {
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Auto
            };
            using var stringReader = new StringReader(text);
            using var reader = XmlReader.Create(stringReader, settings);

            var parts = new List<MarkupPart>();
            var kind = (MarkupPartKind?)null;
            var reference = (Guid?)null;
            var sb = new StringBuilder();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (sb.Length > 0)
                    {
                        parts.Add(new MarkupPart(MarkupPartKind.Whitespace, sb.ToString()));
                        sb.Clear();
                    }

                    if (reader.LocalName == "p")
                    {
                        kind = MarkupPartKind.Punctuation;
                    }
                    else if (reader.LocalName == "k")
                    {
                        kind = MarkupPartKind.Keyword;
                    }
                    else if (reader.LocalName == "n")
                    {
                        kind = MarkupPartKind.LiteralNumber;
                    }
                    else if (reader.LocalName == "s")
                    {
                        kind = MarkupPartKind.LiteralString;
                    }
                    else if (reader.LocalName == "r")
                    {
                        var id = reader.GetAttribute("i");
                        if (id != null)
                            reference = Guid.Parse(id);
                        kind = MarkupPartKind.Reference;
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (kind != null)
                        parts.Add(new MarkupPart(kind.Value, sb.ToString(), reference));

                    kind = null;
                    reference = null;
                    sb.Clear();
                }
                else if (reader.NodeType == XmlNodeType.Text ||
                         reader.NodeType == XmlNodeType.Whitespace ||
                         reader.NodeType == XmlNodeType.SignificantWhitespace)
                {
                    sb.Append(reader.Value);
                }
            }

            return new Markup(parts);
        }
    }
}
