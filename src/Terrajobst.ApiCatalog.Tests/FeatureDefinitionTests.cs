using Terrajobst.ApiCatalog.Features;

namespace Terrajobst.ApiCatalog.Tests;

public class FeatureDefinitionTests
{
    [Fact]
    public void FeatureDefinition_NoDuplicateIds()
    {
        var guidSet = new Dictionary<Guid, FeatureDefinition>();

        foreach (var globalFeature in FeatureDefinition.GlobalFeatures)
        {
            if (!guidSet.TryGetValue(globalFeature.FeatureId, out var existingFeature))
            {
                guidSet.Add(globalFeature.FeatureId, globalFeature);
            }
            else
            {
                Assert.Fail($"The feature '{globalFeature.Name}' has the same GUID as '{existingFeature.Name}'");
            }
        }

        var parameter = Guid.Parse("6769197f-ec9c-4848-af2b-2f50c35630f5");

        foreach (var parameterizedFeature in FeatureDefinition.ApiFeatures)
        {
            var featureId = parameterizedFeature.GetFeatureId(parameter);

            if (!guidSet.TryGetValue(featureId, out var existingFeature))
            {
                guidSet.Add(featureId, parameterizedFeature);
            }
            else
            {
                Assert.Fail($"The feature '{parameterizedFeature.Name}' has the same GUID as '{existingFeature.Name}'");
            }
        }
    }
}