using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

// TODO: Add tests for PreviewDescription

public readonly struct PreviewDescription
{
    public static PreviewDescription? Create(ApiModel api)
    {
        PreviewRequirementModel? apiPreviewRequirement = null;
        ExperimentalModel? apiExperimental = null;
        PackageModel? apiPrereleasePackage = null;
        
        foreach (var declaration in api.Declarations)
        {
            var declarationPreviewRequirement = declaration.GetEffectivePreviewRequirement();
            apiPreviewRequirement ??= declarationPreviewRequirement;

            var declarationExperimental = declaration.GetEffectiveExperimental();
            apiExperimental ??= declarationExperimental;

            PackageModel? declarationPrereleasePackage = null;

            foreach (var (package, _) in declaration.Assembly.Packages)
            {
                var version = NuGetVersion.Parse(package.Version);
                if (version.IsPrerelease)
                    declarationPrereleasePackage = package;
                else
                    declarationPrereleasePackage = null;
            }

            apiPrereleasePackage ??= declarationPrereleasePackage;

            var declarationIsStable = declarationPreviewRequirement is null &&
                                      declarationExperimental is null &&
                                      declarationPrereleasePackage is null;

            if (declarationIsStable)
                return null;
        }

        if (apiPreviewRequirement is not null)
        {
            var description = apiPreviewRequirement.Value.Message;
            var url = apiPreviewRequirement.Value.Url;
            return new PreviewDescription(PreviewReason.MarkedWithRequiresPreviewFeatures, description, url);
        }

        if (apiExperimental is not null)
        {
            var description = $"{apiExperimental.Value.DiagnosticId}: This API is marked as experimental.";
            var url = apiExperimental.Value.Url;
            return new PreviewDescription(PreviewReason.MarkedWithExperimental, description, url);
        }

        if (apiPrereleasePackage is not null)
        {
            var description = $"This API is contained in a prerelease package.";
            return new PreviewDescription(PreviewReason.MarkedWithExperimental, description, null);
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