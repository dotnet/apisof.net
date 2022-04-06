using Microsoft.Cci;
using Microsoft.Cci.Extensions;

using NuGet.Frameworks;

internal sealed class AssemblyTargetFrameworkCompatibility
{
    private readonly string _name;

    private AssemblyTargetFrameworkCompatibility(string name)
    {
        _name = name;
    }

    public override string ToString()
    {
        return _name;
    }

    public static AssemblyTargetFrameworkCompatibility Unknown { get; } = new("Unknown");

    public static AssemblyTargetFrameworkCompatibility Incompatible { get; } = new("Incompatible");

    public static AssemblyTargetFrameworkCompatibility Compatible { get; } = new("Compatible");

    public static AssemblyTargetFrameworkCompatibility CompatibleViaFallback { get; } = new("Compatible via fallback");

    public static AssemblyTargetFrameworkCompatibility Compute(NuGetFramework? assemblyFramework, NuGetFramework framework)
    {
        if (assemblyFramework is null)
            return Unknown;

        // NOTE: This doesn't do any fallback checking because framework isn't FallbackFramework.
        //       We're just hard coding the check for .NET Framework binaries below.
        if (NuGetFrameworkUtility.IsCompatibleWithFallbackCheck(framework, assemblyFramework))
            return Compatible;

        return assemblyFramework.IsDesktop() ? CompatibleViaFallback : Incompatible;
    }
}