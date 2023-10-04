using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

// TODO: Add tests for PreviewDescription

public readonly struct PreviewDescription
{
    public static PreviewDescription? Create(ApiFrameworkAvailability availability)
    {
        var previewRequirement = availability.Declaration.GetEffectivePreviewRequirement();
        if (previewRequirement is not null)
        {
            var description = previewRequirement.Value.Message;
            var url = previewRequirement.Value.Url;
            return new PreviewDescription(PreviewReason.MarkedWithRequiresPreviewFeatures, description, url);
        }

        var experimental = availability.Declaration.GetEffectiveExperimental();
        if (experimental is not null)
        {
            var description = $"{experimental.Value.DiagnosticId}: This API is marked as experimental.";
            var url = experimental.Value.Url;
            return new PreviewDescription(PreviewReason.MarkedWithExperimental, description, url);
        }

        if (availability.Package is not null)
        {
            var version = NuGetVersion.Parse(availability.Package.Value.Version);
            if (version.IsPrerelease)
            {
                var description = $"This API is contained in a prerelease package.";
                return new PreviewDescription(PreviewReason.MarkedWithExperimental, description, null);
            }
        }

        return null;
    }

    public PreviewDescription(PreviewReason reason, string description, string url)
    {
        Reason = reason;
        Description = description;
        Url = url;
    }

    public PreviewReason Reason { get; }
    public string Description { get; }
    public string Url { get; }
}