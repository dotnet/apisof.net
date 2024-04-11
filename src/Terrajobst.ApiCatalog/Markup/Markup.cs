using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Terrajobst.ApiCatalog;

public class Markup
{
    public Markup(IEnumerable<MarkupToken> tokens)
    {
        ThrowIfNull(tokens);

        Tokens = tokens.ToImmutableArray();
    }

    public ImmutableArray<MarkupToken> Tokens { get; }

    public static Markup FromXml(string xml)
    {
        ThrowIfNull(xml);

        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Auto
        };
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, settings);

        var parts = new List<MarkupToken>();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.LocalName == "t")
                {
                    var kindText = reader.GetAttribute("k");
                    var referenceText = reader.GetAttribute("r");
                    var text = reader.GetAttribute("t");
                    var kind = (MarkupTokenKind)int.Parse(kindText!);
                    var reference = referenceText is null ? (Guid?)null : Guid.Parse(referenceText);
                    var token = new MarkupToken(kind, text, reference);
                    parts.Add(token);
                }
                else
                {
                    throw new FormatException($"unexpected element <{reader.LocalName}>");
                }
            }
        }

        return new Markup(parts);
    }

    public override string ToString()
    {
        return string.Concat(Tokens.Select(t => t.Text));
    }
}