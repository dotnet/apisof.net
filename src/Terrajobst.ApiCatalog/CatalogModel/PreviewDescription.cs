using NuGet.Frameworks;
using NuGet.Versioning;

namespace Terrajobst.ApiCatalog;

// TODO: Add tests for PreviewDescription

public readonly struct PreviewDescription
{
    public static PreviewDescription? Create(ApiModel api)
    {
        FrameworkModel? apiPreviewFramework = null;
        PreviewRequirementModel? apiPreviewRequirement = null;
        ExperimentalModel? apiExperimental = null;
        PackageModel? apiPrereleasePackage = null;

        foreach (var declaration in api.Declarations)
        {
            var declarationPreviewRequirement = declaration.GetEffectivePreviewRequirement();
            apiPreviewRequirement ??= declarationPreviewRequirement;

            var declarationExperimental = declaration.GetEffectiveExperimental();
            apiExperimental ??= declarationExperimental;

            var declarationPreviewFramework = declaration.Assembly.Frameworks.Where(fx => fx.IsPreview)
                                                                             .Cast<FrameworkModel?>()
                                                                             .FirstOrDefault();
            apiPreviewFramework ??= declarationPreviewFramework;

            var hasStableInBoxDeclaration = declarationPreviewRequirement is null &&
                                            declarationExperimental is null &&
                                            declarationPreviewFramework is null &&
                                            declaration.Assembly.Frameworks.Any(IsStableFramework);
            if (hasStableInBoxDeclaration)
                return null;

            PackageModel? declarationPrereleasePackage = null;
            var hasAnyStablePackages = false;

            foreach (var (package, _) in declaration.Assembly.Packages)
            {
                var version = NuGetVersion.Parse(package.Version);
                if (version.IsPrerelease)
                {
                    declarationPrereleasePackage = package;
                }
                else
                {
                    declarationPrereleasePackage = null;
                    hasAnyStablePackages = true;
                }
            }

            apiPrereleasePackage ??= declarationPrereleasePackage;

            var hasStablePackage = declarationPrereleasePackage is null &&
                                   hasAnyStablePackages;

            if (hasStablePackage)
                return null;
        }

        if (apiPreviewRequirement is not null)
        {
            var description = apiPreviewRequirement.Value.Message;
            if (string.IsNullOrEmpty(description))
                description = "The API requires turning on platform preview features.";

            var url = apiPreviewRequirement.Value.Url;
            if (string.IsNullOrEmpty(url))
                url = "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2252";

            return new PreviewDescription(PreviewReason.MarkedWithRequiresPreviewFeatures, description, url);
        }

        if (apiExperimental is not null)
        {
            var diagnosticId = apiExperimental.Value.DiagnosticId;
            var description = string.IsNullOrEmpty(diagnosticId)
                                ? "This API is marked as experimental."
                                : $"{diagnosticId}: This API is marked as experimental.";

            var url = apiExperimental.Value.Url;
            if (string.IsNullOrEmpty(url))
                url = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/experimental-attribute";

            return new PreviewDescription(PreviewReason.MarkedWithExperimental, description, url);
        }

        if (apiPreviewFramework is not null)
        {
            var description = "The API is contained in a preview framework.";
            return new PreviewDescription(PreviewReason.FrameworkPreview, description, null);
        }

        if (apiPrereleasePackage is not null)
        {
            var description = "This API is contained in a prerelease package.";
            return new PreviewDescription(PreviewReason.PackagePrerelease, description, null);
        }

        return null;

        static bool IsStableFramework(FrameworkModel fx)
        {
            if (fx.IsPreview)
                return false;

            // For the sake of stability we only consider .NET Framework and .NET Core.
            var nugetFramework = fx.NuGetFramework;
            return string.Equals(nugetFramework.Framework, ".NETFramework", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(nugetFramework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase);
        }
    }

    public PreviewDescription(PreviewReason reason, string description, string? url)
    {
        Reason = reason;
        Description = description;
        Url = url;
    }

    public PreviewReason Reason { get; }
    public string Description { get; }
    public string? Url { get; }

    public override string ToString()
    {
        return Description;
    }
}