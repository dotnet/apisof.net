using NuGet.Packaging.Core;
using NuGet.Versioning;
using Terrajobst.UsageCrawling.Storage;

namespace GenUsageNuGet.Infra;

public sealed class NuGetUsageDatabase : UsageDatabase<PackageIdentity>
{
    private NuGetUsageDatabase(UsageDatabase usageDatabase)
        : base(usageDatabase)
    {
    }

    public static async Task<NuGetUsageDatabase> OpenOrCreateAsync(string fileName)
    {
        ThrowIfNullOrEmpty(fileName);

        var database = await UsageDatabase.OpenOrCreateAsync(fileName);
        return new NuGetUsageDatabase(database);
    }

    protected override PackageIdentity ParseReferenceUnit(string referenceIdentifier)
    {
        ThrowIfNullOrEmpty(referenceIdentifier);

        var indexOfSlash = referenceIdentifier.IndexOf('/');
        if (indexOfSlash < 0)
            throw new FormatException();

        var id = referenceIdentifier.Substring(0, indexOfSlash);
        var versionText = referenceIdentifier.Substring(indexOfSlash + 1);
        var version = NuGetVersion.Parse(versionText);
        return new PackageIdentity(id, version);
    }

    protected override string FormatReferenceUnit(PackageIdentity referenceUnit)
    {
        ThrowIfNull(referenceUnit);

        var id = referenceUnit.Id;
        var versionText = referenceUnit.Version.ToNormalizedString();
        return $"{id}/{versionText}";
    }
}