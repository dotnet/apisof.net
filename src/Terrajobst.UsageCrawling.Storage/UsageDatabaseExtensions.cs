using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.UsageCrawling.Storage;

public static class UsageDatabaseExtensions
{
    public static async Task DeleteIrrelevantFeaturesAsync(this UsageDatabase usageDatabase, ApiCatalogModel catalog)
    {
        ThrowIfNull(usageDatabase);
        ThrowIfNull(catalog);

        var catalogFeatures = FeatureDefinition.GetCatalogFeatures(catalog);
        var storedFeatures = await usageDatabase.GetFeaturesAsync();
        var irrelevantFeatures = storedFeatures.Where(f => !catalogFeatures.ContainsKey(f.Feature)).Select(f => f.Feature).ToArray();
        await usageDatabase.DeleteFeaturesAsync(irrelevantFeatures);
    }

    public static async Task InsertParentsFeaturesAsync(this UsageDatabase usageDatabase, ApiCatalogModel catalog)
    {
        ThrowIfNull(usageDatabase);
        ThrowIfNull(catalog);

        var storedFeatures = await usageDatabase.GetFeaturesAsync();
        var collectorVersionByFeature = storedFeatures.ToDictionary(f => f.Feature, f => f.CollectorVersion);
        var parentFeatures = FeatureDefinition.GetParentFeatures(catalog);

        foreach (var (child, parent) in parentFeatures)
        {
            if (!collectorVersionByFeature.TryGetValue(child, out var childCollectorVersion))
                continue;

            collectorVersionByFeature[parent] = childCollectorVersion;
            await usageDatabase.TryAddFeatureAsync(parent, childCollectorVersion);
            await usageDatabase.AddParentFeatureAsync(child, parent);
        }
    }

    public static Task DeleteIrrelevantFeaturesAsync<T>(this UsageDatabase<T> usageDatabase, ApiCatalogModel catalog)
    {
        ThrowIfNull(usageDatabase);
        ThrowIfNull(catalog);

        return usageDatabase.UnderlyingDatabase.DeleteIrrelevantFeaturesAsync(catalog);
    }

    public static Task InsertParentsFeaturesAsync<T>(this UsageDatabase<T> usageDatabase, ApiCatalogModel catalog)
    {
        ThrowIfNull(usageDatabase);
        ThrowIfNull(catalog);

        return usageDatabase.UnderlyingDatabase.InsertParentsFeaturesAsync(catalog);
    }
}