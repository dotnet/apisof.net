using System.Text;

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