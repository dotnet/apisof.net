using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

internal sealed class MarkupXmlWriter : MarkupWriter
{
    private readonly StringWriter _writer = new();
    private readonly XmlWriter _xmlWriter;

    public MarkupXmlWriter()
    {
        _xmlWriter = new XmlTextWriter(_writer);
    }

    public override void Write(MarkupTokenKind kind, string? text = null, ISymbol? symbol = null)
    {
        _xmlWriter.WriteStartElement("t");

        _xmlWriter.WriteStartAttribute("k");
        _xmlWriter.WriteValue((int)kind);
        _xmlWriter.WriteEndAttribute();
        
        var guid = symbol?.GetCatalogGuid() ?? Guid.Empty;
        if (guid != default)
        {
            _xmlWriter.WriteStartAttribute("r");
            _xmlWriter.WriteValue(guid.ToString("N"));
            _xmlWriter.WriteEndAttribute();
        }

        if (text is not null)
        {
            _xmlWriter.WriteStartAttribute("t");
            _xmlWriter.WriteValue(text);
            _xmlWriter.WriteEndAttribute();
        }
        
        _xmlWriter.WriteEndElement();
    }

    public override string ToString()
    {
        return _writer.ToString();
    }
}