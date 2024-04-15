using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog.Features;

public abstract class FeatureDefinition
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public static GlobalFeatureDefinition DefinesAnyRefStructs { get; } = new DefinesAnyRefStructsDefinition();
    public static GlobalFeatureDefinition DefinesAnyDefaultInterfaceMembers { get; } = new DefinesAnyDefaultInterfaceMembersDefinition();
    public static GlobalFeatureDefinition DefinesAnyVirtualStaticInterfaceMembers { get; } = new DefinesAnyVirtualStaticInterfaceMembersDefinition();
    public static GlobalFeatureDefinition UsesNullableReferenceTypes { get; } = new UsesNullableReferenceTypesDefinition();

    public static IReadOnlyList<GlobalFeatureDefinition> GlobalFeatures { get; } = [
        DefinesAnyRefStructs,
        DefinesAnyDefaultInterfaceMembers,
        DefinesAnyVirtualStaticInterfaceMembers,
        UsesNullableReferenceTypes
    ];

    public static ParameterizedFeatureDefinition<Guid> ApiUsage { get; } = new ApiUsageDefinition();
    public static ParameterizedFeatureDefinition<Guid> DimUsage { get; } = new DimUsageDefinition();

    public static IReadOnlyList<ParameterizedFeatureDefinition<Guid>> ApiFeatures { get; } = [
        ApiUsage,
        DimUsage
    ];

    public static ParameterizedFeatureDefinition<NuGetFramework> TargetFramework { get; } = new TargetFrameworkFeatureDefinition();

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

            foreach (var parent in GetAncestorsAndSelf(child))
            {
                var childFeature = TargetFramework.GetFeatureId(child);
                var parentParent = TargetFramework.GetFeatureId(parent);
                yield return (childFeature, parentParent);
            }
        }
    }

    private static IEnumerable<NuGetFramework> GetAncestorsAndSelf(NuGetFramework framework)
    {
        yield return framework;

        if (framework.HasPlatform)
        {
            if (framework.PlatformVersion != FrameworkConstants.EmptyVersion)
                yield return new NuGetFramework(framework.Framework, framework.Version, framework.Platform, FrameworkConstants.EmptyVersion);

            yield return new NuGetFramework(framework.Framework, framework.Version);
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

    private sealed class UsesNullableReferenceTypesDefinition : GlobalFeatureDefinition
    {
        public override Guid FeatureId { get; } = Guid.Parse("b7977f35-478e-4fef-bc22-9a4984a69a48");

        public override string Name => "Was compiled with nullable reference types";

        public override string Description => "Percentage of applications/packages that compiled with nullable reference types";
    }

    private sealed class ApiUsageDefinition : ParameterizedFeatureDefinition<Guid>
    {
        public override Guid GetFeatureId(Guid api)
        {
            return api;
        }

        public override string Name => "Reference this API";

        public override string Description => "Usage of an API in signatures or method bodies";
    }

    private sealed class DimUsageDefinition : ParameterizedFeatureDefinition<Guid>
    {
        public override Guid GetFeatureId(Guid api)
        {
            return FeatureId.Create(DefinesAnyDefaultInterfaceMembers.FeatureId, api);
        }

        public override string Name => "Declare a DIM for this API";

        public override string Description => "Definition of an interface member with a default implementation (DIM)";
    }

    private sealed class TargetFrameworkFeatureDefinition : ParameterizedFeatureDefinition<NuGetFramework>
    {
        private static readonly Guid TargetFrameworkFeature = Guid.Parse("8fe6904d-e83d-499c-929a-d9dd69fd0b05");

        public override Guid GetFeatureId(NuGetFramework framework)
        {
            var folderName = framework.GetShortFolderName();
            return FeatureId.Create(TargetFrameworkFeature, folderName);
        }

        public override string Name => "Target Framework Usage";

        public override string Description => "Indicates targeting of a specific framework";
    }
}