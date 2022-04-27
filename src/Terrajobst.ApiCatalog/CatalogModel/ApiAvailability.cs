namespace Terrajobst.ApiCatalog;

public sealed class ApiAvailability
{
    internal ApiAvailability(IEnumerable<ApiFrameworkAvailability> frameworks)
    {
        Frameworks = frameworks.ToArray();
    }

    public IReadOnlyList<ApiFrameworkAvailability> Frameworks { get; }
}