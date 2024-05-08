namespace Terrajobst.ApiCatalog;

public sealed class ApiAvailability
{
    internal ApiAvailability(IEnumerable<ApiFrameworkAvailability> frameworks)
    {
        ThrowIfNull(frameworks);

        Frameworks = frameworks.ToArray();
    }

    public IReadOnlyList<ApiFrameworkAvailability> Frameworks { get; }

    public ApiFrameworkAvailability GetCurrent()
    {
        // First we try to pick the highest .NET Core framework

        var result = Frameworks.Where(fx => fx.Framework.Framework == ".NETCoreApp")
                               .OrderByDescending(fx => fx.Framework.Version)
                               .ThenBy(fx => fx.Framework.HasPlatform)
                               .ThenBy(fx => fx.Framework.Platform)
                               .ThenByDescending(fx => fx.Framework.PlatformVersion)
                               .FirstOrDefault();

        // If we couldn't find any, pick the highest version of any framework

        result ??= Frameworks.OrderBy(f => f.Framework.Framework)
                             .ThenByDescending(f => f.Framework.Version)
                             .First();

        return result;
    }
}