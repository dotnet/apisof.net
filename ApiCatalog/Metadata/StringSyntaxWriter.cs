using System.CodeDom.Compiler;
using System.IO;

using Microsoft.CodeAnalysis;

namespace ApiCatalog
{
    internal sealed class StringSyntaxWriter : SyntaxWriter
    {
        private readonly StringWriter _stringWriter;
        private readonly IndentedTextWriter _writer;

        public StringSyntaxWriter()
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
            _writer.Write(text);
        }

        public override void WritePunctuation(string text)
        {
            _writer.Write(text);
        }

        public override void WriteReference(ISymbol symbol, string text)
        {
            _writer.Write(text);
        }

        public override void WriteLiteralString(string text)
        {
            _writer.Write(text);
        }

        public override void WriteLiteralNumber(string text)
        {
            _writer.Write(text);
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
