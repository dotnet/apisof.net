using System.Text;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public readonly struct PlatformAnnotation
{
    public static PlatformAnnotation None { get; } = new(PlatformAnnotationKind.None);

    public static PlatformAnnotation Unrestricted { get; } = new(PlatformAnnotationKind.Unrestricted);

    private PlatformAnnotation(PlatformAnnotationKind kind)
    {
        Kind = kind;
        Entries = Array.Empty<PlatformAnnotationEntry>();
    }

    public PlatformAnnotation(IReadOnlyList<PlatformAnnotationEntry> entries)
    {
        var isAllowList = entries.Any(t => t.Range.IsAllowList);

        Kind = isAllowList ? PlatformAnnotationKind.RestrictedTo : PlatformAnnotationKind.UnrestrictedExceptFor;
        Entries = entries;
    }

    public PlatformAnnotationKind Kind { get; }

    public IReadOnlyList<PlatformAnnotationEntry> Entries { get; }

    public bool IsSupported(string platformName)
    {
        if (Kind is PlatformAnnotationKind.None or PlatformAnnotationKind.Unrestricted)
            return true;

        var (name, version) = PlatformAnnotationContext.ParsePlatform(platformName);

        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
                return entry.Range.IsSupported(version);
        }

        return Kind == PlatformAnnotationKind.UnrestrictedExceptFor;
    }

    public override string ToString()
    {
        switch (Kind)
        {
            case PlatformAnnotationKind.None:
                return $"The selected framework doesn't have platform annotations.";
            case PlatformAnnotationKind.Unrestricted:
                return $"For the selected framework the API is supported on any platform.";
            case PlatformAnnotationKind.UnrestrictedExceptFor:
            {
                var sb = new StringBuilder();
                sb.AppendLine($"For the selected framework the API is supported on any platform except for:");

                foreach (var e in Entries)
                    sb.AppendLine($"- {e}");
                return sb.ToString();
            }
            case PlatformAnnotationKind.RestrictedTo:
            {
                var sb = new StringBuilder();
                sb.AppendLine($"For the selected framework the API is only supported on these platforms:");

                foreach (var e in Entries)
                    sb.AppendLine($"- {e}");

                return sb.ToString();
            }
            default:
                throw new Exception($"Unexpected kind {Kind}");
        }
    }
}