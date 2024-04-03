internal readonly struct FrameworkResult
{
    public FrameworkResult(AvailabilityResult availability,
                           ObsoletionResult? obsoletion,
                           IReadOnlyList<PlatformResult?> platforms)
    {
        ThrowIfNull(platforms);

        Availability = availability;
        Obsoletion = obsoletion;
        Platforms = platforms;
    }

    public AvailabilityResult Availability { get; }

    public ObsoletionResult? Obsoletion { get; }

    public IReadOnlyList<PlatformResult?> Platforms { get; }

    public bool IsRelevant()
    {
        return !Availability.IsAvailable ||
               Availability.Package is not null ||
               Obsoletion is not null ||
               Platforms.Any(p => p?.IsSupported == false);
    }
}