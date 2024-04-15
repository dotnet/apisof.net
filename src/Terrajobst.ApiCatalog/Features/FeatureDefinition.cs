namespace Terrajobst.ApiCatalog.Features;

public abstract class FeatureDefinition
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public static GlobalFeatureDefinition DefinesAnyRefStructs { get; } = new DefinesAnyRefStructsDefinition();
    public static GlobalFeatureDefinition DefinesAnyDefaultInterfaceMembers { get; } = new DefinesAnyDefaultInterfaceMembersDefinition();
    public static GlobalFeatureDefinition DefinesAnyVirtualStaticInterfaceMembers { get; } = new DefinesAnyVirtualStaticInterfaceMembersDefinition();

    public static IReadOnlyList<GlobalFeatureDefinition> GlobalFeatures { get; } = [
        DefinesAnyRefStructs,
        DefinesAnyDefaultInterfaceMembers,
        DefinesAnyVirtualStaticInterfaceMembers
    ];

    public static ApiFeatureDefinition ApiUsage { get; } = new ApiUsageDefinition();
    public static ApiFeatureDefinition DimUsage { get; } = new DimUsageDefinition();

    public static IReadOnlyList<ApiFeatureDefinition> ApiFeatures { get; } = [
        ApiUsage,
        DimUsage
    ];
    
    public static Dictionary<Guid, FeatureDefinition> GetCatalogFeatures(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        var result = new Dictionary<Guid, FeatureDefinition>();

        foreach (var globalFeature in GlobalFeatures)
            result.Add(globalFeature.FeatureId, globalFeature);

        foreach (var api in catalog.AllApis)
        {
            foreach (var apiFeature in ApiFeatures)
                result.Add(apiFeature.GetFeatureId(api.Guid), apiFeature);
        }

        return result;
    }

    public static IEnumerable<(Guid ChildFeatureId, Guid ParentFeatureId)> GetParentFeatures(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        foreach (var child in catalog.AllApis)
        foreach (var parent in child.AncestorsAndSelf())
        foreach (var feature in ApiFeatures)
        {
            var childFeature = feature.GetFeatureId(child.Guid);
            var parentParent = feature.GetFeatureId(parent.Guid);
            yield return (childFeature, parentParent);
        }
    }
    
    private sealed class DefinesAnyRefStructsDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("740841a3-5c09-426a-b43b-750d21250c01");

        public override string Name => "Defines any ref structs";
        
        public override string Description => "Percentage of applications/packages that defined their own ref structs";
    }

    private sealed class DefinesAnyDefaultInterfaceMembersDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("745807b1-d30a-405c-aa91-209bae5f5ea9");

        public override string Name => "Defines any DIMs";
        
        public override string Description => "Percentage of applications/packages that defined any default interface members (DIMs)";
    }

    private sealed class DefinesAnyVirtualStaticInterfaceMembersDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("580c614a-45e8-4f91-a007-322377dd23a9");

        public override string Name => "Defines any virtual static interface members";
        
        public override string Description => "Percentage of applications/packages that defined any virtual static interface members";
    }

    private sealed class ApiUsageDefinition : ApiFeatureDefinition
    {
        public override Guid GetFeatureId(Guid api)
        {
            return api;
        }

        public override string Name => "API Usage";

        public override string Description => "Usage of an API in signatures and method bodies";
    }

    private sealed class DimUsageDefinition : ApiFeatureDefinition
    {
        public override Guid GetFeatureId(Guid api)
        {
            return CombineGuids(DefinesAnyDefaultInterfaceMembers.FeatureId, api);
        }

        public override string Name => "DIM Usage";

        public override string Description => "Definition of an interface member with a default implementation (DIM)";
    }
}