using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog.Features;

namespace ApisOfDotNet.Pages;

public partial class FeatureUsage
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    private IReadOnlyList<(FeatureUsageSource Source, IReadOnlyList<(FeatureDefinition Feature, float Percentage)> Usages)> GetUsages()
    {
        var usages = new List<(FeatureUsageSource Source, FeatureDefinition Feature, float Percentage)>();

        var usageData = CatalogService.UsageData;

        foreach (var feature in FeatureDefinition.GlobalFeatures)
        {
            var featureId = feature.FeatureId;
            foreach (var (usageSource, percentage) in usageData.GetUsage(featureId))
                usages.Add((usageSource, feature, percentage));
        }

        return usages.GroupBy(u => u.Source)
                     .Select(g => (g.Key, (IReadOnlyList<(FeatureDefinition, float)>)g.Select(t => (t.Feature, t.Percentage)).ToArray()))
                     .ToArray();
    }
}