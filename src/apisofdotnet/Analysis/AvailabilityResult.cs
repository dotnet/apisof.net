using Terrajobst.ApiCatalog;

internal struct AvailabilityResult
{
    public static AvailabilityResult Unavailable { get; } = new(isAvailable: false, null);

    public static AvailabilityResult AvailableInBox { get; } = new(isAvailable: true, null);

    public static AvailabilityResult AvailableInPackage(PackageModel package)
    {
        return new AvailabilityResult(isAvailable: true, package);
    }

    private AvailabilityResult(bool isAvailable,
                               PackageModel? package)
    {
        IsAvailable = isAvailable;
        Package = package;
    }

    public bool IsAvailable { get; }

    public PackageModel? Package { get; }

    public override string ToString()
    {
        return !IsAvailable
            ? "Unavailable"
            : Package is null
                    ? "Available"
                    : $"Available via package {Package.Value.Name}";
    }
}