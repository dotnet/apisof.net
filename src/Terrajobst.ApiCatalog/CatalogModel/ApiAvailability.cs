namespace Terrajobst.ApiCatalog;

public sealed class ApiAvailability
{
    internal ApiAvailability(IEnumerable<ApiFrameworkAvailability> frameworks)
    {
        ThrowIfNull(frameworks);

        Frameworks = frameworks.ToArray();
    }

    public IReadOnlyList<ApiFrameworkAvailability> Frameworks { get; }
}