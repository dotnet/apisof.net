using System.Diagnostics;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp;
using Terrajobst.UsageCrawling.Collectors;
using Terrajobst.UsageCrawling.Tests.Infra;

namespace Terrajobst.UsageCrawling.Tests.Collectors;

public class TargetFrameworkCollectorTests : CollectorTest<TargetFrameworkCollector>
{
    [Fact]
    public void TargetFrameworkCollector_PrefersTargetFrameworkOver_References()
    {
        var source =
            """
            using System.Runtime.Versioning;
            [assembly: TargetFrameworkAttribute(".NETCoreApp, Version=5.1")]
            """;

        Check(TargetFramework.Net80, source, [FeatureUsage.ForTargetFramework("net5.1")]);
    }

    [Fact]
    public void TargetFrameworkCollector_Infers_NetCoreApp80()
    {
        Check(TargetFramework.Net80, string.Empty, [FeatureUsage.ForTargetFramework("net8.0")]);
    }

    [Fact]
    public void TargetFrameworkCollector_Infers_NetCoreApp31()
    {
        Check(TargetFramework.NetCoreApp31, string.Empty, [FeatureUsage.ForTargetFramework("netcoreapp3.1")]);
    }

    [Fact]
    public void TargetFrameworkCollector_Infers_Net40()
    {
        Check(TargetFramework.Net472, string.Empty, [FeatureUsage.ForTargetFramework("net40")]);
    }

    [Fact]
    public void TargetFrameworkCollector_Infers_NetStandard20()
    {
        Check(TargetFramework.NetStandard20, string.Empty, [FeatureUsage.ForTargetFramework("netstandard2.0")]);
    }

    [Fact]
    public void TargetFrameworkCollector_Infers_NetStandard13()
    {
        Check(TargetFramework.NetStandard13, string.Empty, [FeatureUsage.ForTargetFramework("netstandard1.3")]);
    }

    private enum TargetFramework
    {
        Net80,
        Net472,
        NetStandard20,
        NetStandard13,
        NetCoreApp31
    }

    private void Check(TargetFramework framework, string source, IEnumerable<FeatureUsage> expectedUsages)
    {
        _targetFramework = framework;
        Check(source, expectedUsages);
    }

    private TargetFramework _targetFramework;

    protected override CSharpCompilation ModifyCompilation(CSharpCompilation compilation)
    {
        return _targetFramework switch {
            TargetFramework.Net80 => compilation.WithReferences(Net80.References.All),
            TargetFramework.Net472 => compilation.WithReferences(Net472.References.All),
            TargetFramework.NetStandard20 => compilation.WithReferences(NetStandard20.References.All),
            TargetFramework.NetStandard13 => compilation.WithReferences(NetStandard13.References.All),
            TargetFramework.NetCoreApp31 => compilation.WithReferences(NetCoreApp31.References.All),
            _ => throw new UnreachableException($"Unmapped target framework: {_targetFramework}")
        };
    }
}