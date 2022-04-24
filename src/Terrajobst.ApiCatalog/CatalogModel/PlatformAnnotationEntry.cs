namespace Terrajobst.ApiCatalog;

public readonly struct PlatformAnnotationEntry
{
    public PlatformAnnotationEntry(string name, PlatformSupportRange range)
    {
        Name = name;
        Range = range;
    }

    public string Name { get; }

    public PlatformSupportRange Range { get; }

    public override string ToString()
    {
        if (Range.IsEmpty || Range.AllVersions)
            return FormatPlatform(Name);

        return $"{FormatPlatform(Name)}: {Range}";
    }

    private static string FormatPlatform(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "android" => "Android",
            "browser" => "Browser",
            "freebsd" => "FreeBSD",
            "illumos" => "illumos",
            "ios" => "iOS",
            "linux" => "Linux",
            "maccatalyst" => "Mac Catalyst",
            "macos" => "macOS",
            "solaris" => "Solaris",
            "tvos" => "tvOS",
            "watchos" => "watchOS",
            "windows" => "Windows",
            _ => name
        };
    }
}