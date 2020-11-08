using System;
using System.CodeDom.Compiler;
using System.IO;

using Microsoft.CodeAnalysis;

namespace PackageIndexing
{
    internal sealed class MarkupSyntaxWriter : SyntaxWriter
    {
        private readonly StringWriter _stringWriter;
        private readonly IndentedTextWriter _writer;

        public MarkupSyntaxWriter()
        {
            _stringWriter = new StringWriter();
            _writer = new IndentedTextWriter(_stringWriter);
        }

        public override int Indent
        {
            get => _writer.Indent;
            set => _writer.Indent = value;
        }

        public override void WriteKeyword(string text)
        {
            _writer.Write("<k>");
            _writer.Write(text);
            _writer.Write("</k>");
        }

        public override void WriteLiteralNumber(string text)
        {
            _writer.Write("<n>");
            _writer.Write(text);
            _writer.Write("</n>");
        }

        public override void WriteLiteralString(string text)
        {
            _writer.Write("<s>");
            _writer.Write(text);
            _writer.Write("</s>");
        }

        public override void WritePunctuation(string text)
        {
            _writer.Write("<p>");
            _writer.Write(text);
            _writer.Write("</p>");
        }

        public override void WriteReference(ISymbol symbol, string text)
        {
            var guid = symbol?.GetCatalogGuid() ?? Guid.Empty;
            if (guid == Guid.Empty)
            {
                _writer.Write(text);
            }
            else
            {
                _writer.Write("<r i=\"");
                _writer.Write(guid.ToString("N"));
                _writer.Write("\">");
                _writer.Write(text);
                _writer.Write("</r>");
            }
        }

        public override void WriteSpace()
        {
            _writer.Write(" ");
        }

        public override void WriteLine()
        {
            _writer.WriteLine();
        }

        public override string ToString()
        {
            _writer.Flush();
            return _stringWriter.ToString();
        }
    }
}
