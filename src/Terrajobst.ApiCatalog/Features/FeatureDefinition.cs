using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog.Features;

public abstract class FeatureDefinition
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public static GlobalFeatureDefinition DefinesAnyRefStructs { get; } = new DefinesAnyRefStructsDefinition();
    public static GlobalFeatureDefinition DefinesAnyDefaultInterfaceMembers { get; } = new DefinesAnyDefaultInterfaceMembersDefinition();
    public static GlobalFeatureDefinition DefinesAnyVirtualStaticInterfaceMembers { get; } = new DefinesAnyVirtualStaticInterfaceMembersDefinition();
    public static GlobalFeatureDefinition DefinesAnyRefFields { get; } = new DefinesAnyRefFieldsDefinition();
    public static GlobalFeatureDefinition UsesNullableReferenceTypes { get; } = new UsesNullableReferenceTypesDefinition();

    public static IReadOnlyList<GlobalFeatureDefinition> GlobalFeatures { get; } = [
        DefinesAnyRefStructs,
        DefinesAnyDefaultInterfaceMembers,
        DefinesAnyVirtualStaticInterfaceMembers,
        DefinesAnyRefFields,
        UsesNullableReferenceTypes
    ];

    public static ParameterizedFeatureDefinition<Guid> ReferencesApi { get; } = new ReferencesApiDefinition();
    public static ParameterizedFeatureDefinition<Guid> DefinesDim { get; } = new DefinesDimDefinition();
    public static ParameterizedFeatureDefinition<Guid> DerivesFromType { get; } = new DerivesFromUsageDefinition();
    public static ParameterizedFeatureDefinition<Guid> ReadsField { get; } = new ReadsFieldDefinition();
    public static ParameterizedFeatureDefinition<Guid> WritesField { get; } = new WritesFieldDefinition();
    public static ParameterizedFeatureDefinition<Guid> ThrowsException { get; } = new ThrowsExceptionDefinition();
    public static ParameterizedFeatureDefinition<Guid> CatchesException { get; } = new CatchesExceptionDefinition();

    public static IReadOnlyList<ParameterizedFeatureDefinition<Guid>> ApiFeatures { get; } = [
        ReferencesApi,
        DefinesDim,
        DerivesFromType,
        ReadsField,
        WritesField,
        ThrowsException,
        CatchesException
    ];

    public static ParameterizedFeatureDefinition<NuGetFramework> TargetFramework { get; } = new TargetFrameworkFeatureDefinition();

    // Synthetic features
    //
    // These aren't collected but exposed by looking at the children of properties/events.

    public static FeatureDefinition GetProperty { get; } = new PropertyGetFeatureDefinition();
    public static FeatureDefinition SetProperty { get; } = new PropertySetFeatureDefinition();
    public static FeatureDefinition AddEvent { get; } = new EventAddFeatureDefinition();
    public static FeatureDefinition RemoveEvent { get; } = new EventRemoveFeatureDefinition();

    public static Dictionary<Guid, FeatureDefinition> GetCatalogFeatures(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        var result = new Dictionary<Guid, FeatureDefinition>();

        // Global features

        foreach (var globalFeature in GlobalFeatures)
            result.Add(globalFeature.FeatureId, globalFeature);

        // API features

        foreach (var api in catalog.AllApis)
        {
            foreach (var apiFeature in ApiFeatures)
                result.Add(apiFeature.GetFeatureId(api.Guid), apiFeature);
        }

        // Other parameterized features

        foreach (var framework in catalog.Frameworks)
            result.Add(TargetFramework.GetFeatureId(framework.NuGetFramework), TargetFramework);

        return result;
    }

    public static IEnumerable<(Guid ChildFeatureId, Guid ParentFeatureId)> GetParentFeatures(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        // API features

        foreach (var child in catalog.AllApis)
        foreach (var parent in child.AncestorsAndSelf())
        foreach (var feature in ApiFeatures)
        {
            var childFeature = feature.GetFeatureId(child.Guid);
            var parentParent = feature.GetFeatureId(parent.Guid);
            yield return (childFeature, parentParent);
        }

        // Target frameworks

        foreach (var fx in catalog.Frameworks)
        {
            var child = fx.NuGetFramework;
            if (!child.IsRelevantForCatalog())
                continue;

            foreach (var parent in TargetFrameworkHierarchy.GetAncestorsAndSelf(child))
            {
                var childFeature = TargetFramework.GetFeatureId(child);
                var parentParent = TargetFramework.GetFeatureId(parent);
                yield return (childFeature, parentParent);
            }
        }
    }

    private sealed class DefinesAnyRefStructsDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("740841a3-5c09-426a-b43b-750d21250c01");

        public override string Name => "Define any ref structs";

        public override string Description => "Percentage of applications/packages that defined their own ref structs";
    }

    private sealed class DefinesAnyDefaultInterfaceMembersDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("745807b1-d30a-405c-aa91-209bae5f5ea9");

        public override string Name => "Define any DIMs";

        public override string Description => "Percentage of applications/packages that defined any default interface members (DIMs)";
    }

    private sealed class DefinesAnyVirtualStaticInterfaceMembersDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("580c614a-45e8-4f91-a007-322377dd23a9");

        public override string Name => "Define any virtual static interface members";

        public override string Description => "Percentage of applications/packages that defined any virtual static interface members";
    }

    private sealed class DefinesAnyRefFieldsDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("acb7b28c-c88a-4bc6-b099-08c7e35cc1c1");

        public override string Name => "Define any ref fields";

        public override string Description => "Percentage of applications/packages that defined any ref fields";
    }

    private sealed class UsesNullableReferenceTypesDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("b7977f35-478e-4fef-bc22-9a4984a69a48");

        public override string Name => "Compile with nullable reference types";

        public override string Description => "Percentage of applications/packages that compiled with nullable reference types";
    }

    private sealed class ReferencesApiDefinition : ParameterizedFeatureDefinition<Guid>
    {
        public override Guid GetFeatureId(Guid api)
        {
            return api;
        }

        public override string Name => "Reference this API";

        public override string Description => "Usage of an API in signatures or method bodies";
    }

    private sealed class DefinesDimDefinition : ParameterizedFeatureDefinition<Guid>
    {
        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DefinesAnyDefaultInterfaceMembers.FeatureId, api);
        }

        public override string Name => "Declare a DIM for this API";

        public override string Description => "Definition of an interface member with a default implementation (DIM)";
    }

    private sealed class DerivesFromUsageDefinition : ParameterizedFeatureDefinition<Guid>
    {
        private static readonly Guid DerivesFromFeature = Guid.Parse("ee8100f2-6eed-4e31-b290-49941837f241");

        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DerivesFromFeature, api);
        }

        public override string Name => "Derive from this class or interface";

        public override string Description => "Subclassing or interface implementation";
    }

    private sealed class ReadsFieldDefinition : ParameterizedFeatureDefinition<Guid>
    {
        private static readonly Guid DerivesFromFeature = Guid.Parse("1cfae67f-df1c-42df-9a6e-f3b13bff730e");

        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DerivesFromFeature, api);
        }

        public override string Name => "Read field";

        public override string Description => "Reads from a field";
    }

    private sealed class WritesFieldDefinition : ParameterizedFeatureDefinition<Guid>
    {
        private static readonly Guid DerivesFromFeature = Guid.Parse("c014a47a-29d8-4d61-a538-dc7b6ce33ed4");

        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DerivesFromFeature, api);
        }

        public override string Name => "Write field";

        public override string Description => "Writes to a field";
    }

    private sealed class ThrowsExceptionDefinition : ParameterizedFeatureDefinition<Guid>
    {
        private static readonly Guid DerivesFromFeature = Guid.Parse("eb23985b-8fe8-4230-9fa8-15c21827f5ee");

        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DerivesFromFeature, api);
        }

        public override string Name => "Throw exception";

        public override string Description => "Throwing of this exception type";
    }

    private sealed class CatchesExceptionDefinition : ParameterizedFeatureDefinition<Guid>
    {
        private static readonly Guid DerivesFromFeature = Guid.Parse("6b75066b-1e1e-47d7-854e-fe3da867ad0d");

        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DerivesFromFeature, api);
        }

        public override string Name => "Catch exception";

        public override string Description => "Catch handlers for this exception type";
    }

    private sealed class TargetFrameworkFeatureDefinition : ParameterizedFeatureDefinition<NuGetFramework>
    {
        private static readonly Guid TargetFrameworkFeature = Guid.Parse("3c1be14d-bddb-474a-88e1-2d3605a8be6d");

        public override Guid GetFeatureId(NuGetFramework framework)
        {
            var folderName = framework.GetShortFolderName();
            return FeatureId.Create(TargetFrameworkFeature, folderName);
        }

        public override string Name => "Target framework";

        public override string Description => "Indicates targeting of a specific framework";
    }

    private sealed class PropertyGetFeatureDefinition : FeatureDefinition
    {
        public override string Name => "Get property";
        public override string Description => "Percentage of applications/packages that read this property";
    }

    private sealed class PropertySetFeatureDefinition : FeatureDefinition
    {
        public override string Name => "Set property";
        public override string Description => "Percentage of applications/packages that write this property";
    }

    private sealed class EventAddFeatureDefinition : FeatureDefinition
    {
        public override string Name => "Subscribe to this event";
        public override string Description => "Percentage of applications/packages that subscribe to this event";
    }

    private sealed class EventRemoveFeatureDefinition : FeatureDefinition
    {
        public override string Name => "Unsubscribe from this event";
        public override string Description => "Percentage of applications/packages that unsubscribe to this event";
    }
}
