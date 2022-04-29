internal readonly struct PlatformResult
{
    public static PlatformResult Supported => new(true);

    public static PlatformResult Unsupported => new(false);

    private PlatformResult(bool isSupported)
    {
        IsSupported = isSupported;
    }

    public bool IsSupported { get; }

    public override string ToString()
    {
        return IsSupported ? "Supported" : "Unsupported";
    }
}