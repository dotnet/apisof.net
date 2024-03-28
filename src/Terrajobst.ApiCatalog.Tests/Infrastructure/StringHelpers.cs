namespace Terrajobst.ApiCatalog.Tests;

internal static class StringHelpers
{
    public static string[] SplitLines(this string text)
    {
        var lines = new List<string>();

        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
            lines.Add(line);

        return lines.ToArray();
    }
}