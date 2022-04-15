using System.Text;

internal sealed class CsvWriter : IDisposable
{
    private static readonly char[] CharactersThatNeedEscaping = { ',', '"', '\r', '\n' };

    private readonly TextWriter _textWriter;
    private bool _valuesSeen;

    public CsvWriter(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    public CsvWriter(string path)
        : this(new StreamWriter(path))
    {
    }

    public void Dispose()
    {
        _textWriter.Dispose();
    }

    public void Write()
    {
        Write(string.Empty);
    }

    public void Write(object? value)
    {
        if (_valuesSeen)
            _textWriter.Write(',');

        _valuesSeen = true;
        var text = value?.ToString() ?? string.Empty;
        var escapedText = EscapeValue(text);
        _textWriter.Write(escapedText);
    }

    public void WriteLine()
    {
        if (_valuesSeen)
        {
            _valuesSeen = false;
            _textWriter.WriteLine();
        }
    }

    private static string EscapeValue(string value)
    {
        var needsEscaping = value.IndexOfAny(CharactersThatNeedEscaping) >= 0;
        if (!needsEscaping)
            return value;

        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            if (c == '"')
                sb.Append('"');

            sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }
}