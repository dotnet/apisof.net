using Terrajobst.ApiCatalog;

namespace Terrajobst.NetUpgradePlanner;

public readonly struct PlatformSet : IComparable, IComparable<PlatformSet>
{
    public static PlatformSet Any { get; } = new PlatformSet(null);

    public static PlatformSet For(IEnumerable<string> platforms)
    {
        return new PlatformSet(platforms.ToArray());
    }

    private PlatformSet(IReadOnlyList<string>? relevantPlatforms)
    {
        _relevantPlatforms = relevantPlatforms;
    }

    private readonly IReadOnlyList<string>? _relevantPlatforms;

    public bool IsAny => _relevantPlatforms is null or { Count: 0 };

    public bool IsSpecific => _relevantPlatforms is { Count: > 0 };

    public IReadOnlyList<string> Platforms => _relevantPlatforms ?? Array.Empty<string>();

    public static bool TryParse(string text, out PlatformSet value)
    {
        if (string.IsNullOrEmpty(text) || string.Equals(text, "Any", StringComparison.OrdinalIgnoreCase))
        {
            value = Any;
            return true;
        };

        var platforms = text.Split(',', StringSplitOptions.TrimEntries);
        value = For(platforms);
        return true;
    }

    public static PlatformSet Parse(string text)
    {
        if (!TryParse(text, out var result))
            throw new FormatException();

        return result;
    }

    public int CompareTo(object? obj)
    {
        return obj is PlatformSet other ? CompareTo(other) : -1;
    }

    public int CompareTo(PlatformSet other)
    {
        return string.Compare(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public string ToDisplayString()
    {
        if (IsAny)
            return "Any";

        return string.Join(",", _relevantPlatforms!.Select(PlatformAnnotationEntry.FormatPlatform));
    }

    public override string ToString()
    {
        return string.Join(",", _relevantPlatforms ?? Enumerable.Empty<string>());
    }
}
