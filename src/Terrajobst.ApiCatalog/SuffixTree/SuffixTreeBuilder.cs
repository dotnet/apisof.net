using System.Text;

namespace Terrajobst.ApiCatalog;

public sealed class SuffixTreeBuilder
{
    private readonly Node _root = new();

    public void Add(string key, int value)
    {
        var tokens = Tokenizer.Tokenize(key).ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] != ".")
                Add(_root, tokens, i, value);
        }
    }

    private static void Add(Node node, string[] tokens, int tokenIndex, int value)
    {
        var text = tokens[tokenIndex];
        var childIndex = node.GetChildIndex(text);

        if (childIndex < 0)
        {
            childIndex = ~childIndex;
            var child = new Node { Text = text };
            node.InsertChild(childIndex, child);
        }

        if (tokenIndex < tokens.Length - 1)
        {
            Add(node.Children[childIndex], tokens, tokenIndex + 1, value);
        }
        else
        {
            var tokenOffset = tokens.Sum(t => t.Length) - tokens.Last().Length;
            node.Children[childIndex].AddValue(tokenOffset, value);
        }
    }

    public ReadOnlySpan<int> Lookup(string key)
    {
        var tokens = Tokenizer.Tokenize(key).ToArray();

        var node = _root;

        foreach (var token in tokens)
        {
            var childIndex = node.GetChildIndex(token);

            if (childIndex == -1)
                return null;

            node = node.Children[childIndex];
        }

        return null;
    }

    public void WriteDot(TextWriter writer)
    {
        var idByNode = new Dictionary<Node, string>();
        var stack = new Stack<Node>();
        stack.Push(_root);

        while (stack.Count > 0)
        {
            var n = stack.Pop();

            idByNode.Add(n, $"N{idByNode.Count}");

            if (n.Values.Any())
                idByNode[n] += $"_V{n.Values.Count}";

            foreach (var child in n.Children)
                stack.Push(child);
        }

        writer.WriteLine("digraph {");

        stack.Push(_root);

        while (stack.Count > 0)
        {
            var from = stack.Pop();
            var fromId = idByNode[from];
            foreach (var to in from.Children)
            {
                var toId = idByNode[to];
                writer.WriteLine($"    {fromId} -> {toId} [label = \"{to.Text}\"]");
                stack.Push(to);
            }
        }

        writer.WriteLine("}");
    }

    public void WriteSuffixTree(Stream stream)
    {
        // header
        //   magic value = int[4]
        //   version = ushort
        //   strings = start: int, length: int
        //   nodes = start: int, length: int
        //   rootOffset = int
        // strings
        //   string...
        //     length: int, byte...
        // nodes
        //   node...
        //     text = int
        //     children = count:int, int...
        //     values = count:int
        //          value = offset: int, value: int...

        var stringOffsets = new Dictionary<string, int>();
        var nodeOffsets = new Dictionary<Node, int>();

        using var strings = new MemoryStream();
        using var nodes = new MemoryStream();

        using var stringWriter = new BinaryWriter(strings, Encoding.UTF8);
        using var nodeWriter = new BinaryWriter(nodes, Encoding.UTF8);

        WriteNode(_root);

        stringWriter.Flush();
        nodeWriter.Flush();
        strings.Position = 0;
        nodes.Position = 0;

        using var headerWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        var stringStart = 4 + 2 + 4 + 4 + 4 + 4 + 4;
        var stringLength = checked((int)strings.Length);
        var nodesStart = stringStart + stringLength;
        var nodesLength = checked((int)nodes.Length);

        headerWriter.Write(SuffixTree.MagicNumbers);
        headerWriter.Write(SuffixTree.Version);
        headerWriter.Write(stringStart);
        headerWriter.Write(stringLength);
        headerWriter.Write(nodesStart);
        headerWriter.Write(nodesLength);
        headerWriter.Write(nodeOffsets[_root]);
        headerWriter.Flush();

        strings.CopyTo(stream);
        nodes.CopyTo(stream);

        void WriteNode(Node node)
        {
            foreach (var child in node.Children)
                WriteNode(child);

            var nodeOffset = checked((int)nodes.Position);
            nodeOffsets.Add(node, nodeOffset);

            if (!stringOffsets.TryGetValue(node.Text, out var stringOffset))
            {
                stringOffset = checked((int)strings.Position);
                var utf8Bytes = Encoding.UTF8.GetBytes(node.Text);
                stringWriter.Write(utf8Bytes.Length);
                stringWriter.Write(utf8Bytes);
                stringOffsets.Add(node.Text, stringOffset);
            }

            nodeWriter.Write(stringOffset);

            nodeWriter.Write(node.Children.Count);
            foreach (var child in node.Children)
                nodeWriter.Write(nodeOffsets[child]);

            nodeWriter.Write(node.Values.Count);
            foreach (var offsetValue in node.Values)
            {
                nodeWriter.Write(offsetValue.Offset);
                nodeWriter.Write(offsetValue.Value);
            }

            nodeWriter.Flush();
        }
    }

    public SuffixTree Build()
    {
        using var stream = new MemoryStream();
        WriteSuffixTree(stream);
        var buffer = stream.ToArray();
        return SuffixTree.Load(buffer);
    }

    private class Node
    {
        private string _text;
        private List<Node> _children;
        private List<(int Offset, int Value)> _values;

        public string Text { get => _text ?? string.Empty; set => _text = value; }
        public IReadOnlyList<Node> Children => (IReadOnlyList<Node>)_children ?? Array.Empty<Node>();
        public IReadOnlyList<(int Offset, int Value)> Values => (IReadOnlyList<(int, int)>)_values ?? Array.Empty<(int, int)>();

        public void InsertChild(int index, Node node)
        {
            if (_children is null)
                _children = new List<Node>();

            _children.Insert(index, node);
        }

        public void AddValue(int offset, int value)
        {
            if (_values is null)
                _values = new List<(int, int)>();

            _values.Add((offset, value));
        }

        public int GetChildIndex(string text)
        {
            if (_children is null)
                return -1;

            var lo = 0;
            var hi = Children.Count - 1;

            while (lo <= hi)
            {
                var i = (lo + hi) / 2;

                var c = string.Compare(Children[i].Text, text, StringComparison.Ordinal);
                if (c == 0)
                    return i;
                if (c < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return ~lo;
        }
    }
}