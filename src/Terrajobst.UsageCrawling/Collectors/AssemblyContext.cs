using NuGet.Frameworks;
using NuGet.Packaging;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class AssemblyContext
{
    public static AssemblyContext Empty { get; } = new(); 

    public PackageReaderBase? Package { get; init; }
    
    public NuGetFramework? Framework { get; init; }
}