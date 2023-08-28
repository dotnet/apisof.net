using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terrajobst.ApiCatalog.Tests;

internal static class StringHelpers
{
    public static string Unindent(this string text)
    {
        var minIndent = int.MaxValue;

        var lines = SplitLines(text);
        foreach (var line in lines)
        {
            var trimmedStart = line.TrimStart();
            if (trimmedStart.Length > 0)
            {
                var indent = line.Length - trimmedStart.Length;
                minIndent = Math.Min(minIndent, indent);
            }
        }

        if (minIndent == 0)
            return text;

        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var isBlank = line.TrimStart().Length == 0;
            var unindentedLine = isBlank ? line : line[minIndent..];
            sb.AppendLine(unindentedLine);
        }

        return sb.ToString().Trim();
    }

    public static string[] SplitLines(this string text)
    {
        var lines = new List<string>();

        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
            lines.Add(line);

        return lines.ToArray();
    }
}