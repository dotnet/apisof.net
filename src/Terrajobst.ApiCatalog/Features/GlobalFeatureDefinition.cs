namespace Terrajobst.ApiCatalog.Features;

public abstract class GlobalFeatureDefinition : FeatureDefinition
{
    public abstract Guid FeatureId { get; }
}