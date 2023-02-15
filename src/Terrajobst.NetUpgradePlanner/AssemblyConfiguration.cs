using System.Collections.Immutable;

namespace Terrajobst.NetUpgradePlanner;

public sealed class AssemblyConfiguration
{
    public static AssemblyConfiguration Empty { get; } = new();

    private readonly ImmutableDictionary<AssemblySetEntry, string> _desiredFrameworks;
    private readonly ImmutableDictionary<AssemblySetEntry, PlatformSet> _desiredPlatforms;

    private AssemblyConfiguration()
        : this(ImmutableDictionary<AssemblySetEntry, string>.Empty,
               ImmutableDictionary<AssemblySetEntry, PlatformSet>.Empty)
    {
    }

    private AssemblyConfiguration(ImmutableDictionary<AssemblySetEntry, string> desiredFrameworks,
                                  ImmutableDictionary<AssemblySetEntry, PlatformSet> desiredPlatforms)
    {
        _desiredFrameworks = desiredFrameworks;
        _desiredPlatforms = desiredPlatforms;
    }

    public AssemblyConfiguration RemoveRange(IEnumerable<AssemblySetEntry> assemblies)
    {
        var desiredFrameworks = _desiredFrameworks.RemoveRange(assemblies);
        var desiredPlatforms = _desiredPlatforms.RemoveRange(assemblies);

        if (desiredFrameworks == _desiredFrameworks &&
            desiredPlatforms == _desiredPlatforms)
            return this;

        return new AssemblyConfiguration(desiredFrameworks, desiredPlatforms);
    }

    public string GetDesiredFramework(AssemblySetEntry assembly)
    {
        return _desiredFrameworks[assembly];
    }

    public PlatformSet GetDesiredPlatforms(AssemblySetEntry assembly)
    {
        return _desiredPlatforms[assembly];
    }

    public AssemblyConfiguration SetDesiredFramework(AssemblySetEntry assembly, string desiredFramework)
    {
        return WithDesiredFrameworks(_desiredFrameworks.SetItem(assembly, desiredFramework));
    }

    public AssemblyConfiguration SetDesiredPlatforms(AssemblySetEntry assembly, PlatformSet desiredPlatforms)
    {
        return WithDesiredPlatforms(_desiredPlatforms.SetItem(assembly, desiredPlatforms));
    }

    private AssemblyConfiguration WithDesiredFrameworks(ImmutableDictionary<AssemblySetEntry, string> value)
    {
        if (value == _desiredFrameworks)
            return this;

        return new AssemblyConfiguration(value, _desiredPlatforms);
    }

    private AssemblyConfiguration WithDesiredPlatforms(ImmutableDictionary<AssemblySetEntry, PlatformSet> value)
    {
        if (value == _desiredPlatforms)
            return this;

        return new AssemblyConfiguration(_desiredFrameworks, value);
    }
}
