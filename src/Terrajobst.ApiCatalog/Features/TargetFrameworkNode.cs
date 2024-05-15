using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog.Features;

public sealed class TargetFrameworkNode
{
    internal TargetFrameworkNode(TargetFrameworkNode? parent, NuGetFramework framework, Func<TargetFrameworkNode, IEnumerable<TargetFrameworkNode>> childrenFactory)
    {
        ThrowIfNull(framework);
        ThrowIfNull(childrenFactory);

        Parent = parent;
        Level = parent?.Level + 1 ?? 0;
        Framework = framework;
        Children = childrenFactory(this).ToArray();
    }

    public TargetFrameworkNode? Parent { get; }

    public NuGetFramework Framework { get; }

    public int Level { get; }

    public string Name
    {
        get
        {
            if (Framework.HasPlatform)
            {
                if (Framework.PlatformVersion == FrameworkConstants.EmptyVersion)
                    return PlatformAnnotationEntry.FormatPlatform(Framework.Platform);
                else
                    return Framework.PlatformVersion.GetVersionDisplayString();
            }
            else
            {
                if (Framework.Version == FrameworkConstants.EmptyVersion)
                    return Framework.GetFrameworkDisplayString();
                else
                    return Framework.GetVersionDisplayString();
            }
        }
    }

    public IReadOnlyList<TargetFrameworkNode> Children { get; }
}
