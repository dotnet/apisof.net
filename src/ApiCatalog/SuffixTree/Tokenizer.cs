using System.Collections.Generic;

namespace ApiCatalog
{
    public static class Tokenizer
    {
        public static IEnumerable<string> Tokenize(string text)
        {
            if (text.Contains("("))
                text = text.Substring(0, text.IndexOf('('));

            // TODO: This doesn't work, we need to keep the member of generic types
            if (text.Contains("<"))
                text = text.Substring(0, text.IndexOf('<'));

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
}