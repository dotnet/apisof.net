namespace Terrajobst.ApiCatalog.Features;

public abstract class ParameterizedFeatureDefinition<T> : FeatureDefinition
{
    public abstract Guid GetFeatureId(T parameter);
}