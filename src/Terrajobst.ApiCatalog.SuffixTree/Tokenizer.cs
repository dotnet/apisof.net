namespace Terrajobst.ApiCatalog;

public static class Tokenizer
{
    public static IEnumerable<string> Tokenize(string text)
    {
        ThrowIfNull(text);

        if (text.Contains("("))
            text = text.Substring(0, text.IndexOf('('));

        // TODO: We'd like to keep generic types for typing purposes.
        while (text.Contains("<"))
        {
            var indexOfFirstOpenBracket = text.IndexOf('<');
            var indexOfFirstCloseBracket = text.IndexOf('>', indexOfFirstOpenBracket);
            if (indexOfFirstCloseBracket < 0)
            {
                // This will happen for operator less than.
                break;
            }

            var bracketLength = indexOfFirstCloseBracket - indexOfFirstOpenBracket + 1;
            text = text.Remove(indexOfFirstOpenBracket, bracketLength);
        }

        var start = 0;
        var position = 0;
        var seenLowerCase = false;

        while (position < text.Length)
        {
            if (text[position] == '.')
            {
                yield return text[start..position].ToLowerInvariant();
                yield return ".";
                position++;
                start = position;
            }
            else if (char.IsLower(text[position]) || char.IsDigit(text[position]))
            {
                seenLowerCase = true;
                position++;
            }
            else if (char.IsUpper(text[position]))
            {
                if (seenLowerCase)
                {
                    if (position > start)
                        yield return text[start..position].ToLowerInvariant();
                    start = position;
                    seenLowerCase = false;
                }

                position++;
            }
            else
            {
                position++;
            }
        }

        if (start < position)
            yield return text[start..].ToLowerInvariant();
    }
}