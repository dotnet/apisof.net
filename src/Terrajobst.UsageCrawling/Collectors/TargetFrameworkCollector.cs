using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using NuGet.Frameworks;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class TargetFrameworkCollector : IncrementalUsageCollector
{
    public override int VersionRequired => 5;

    protected override void CollectFeatures(IAssembly assembly, AssemblyContext assemblyContext, Context context)
    {
        var framework = assemblyContext.Framework ?? InferFramework(assembly);
        if (framework is not null)
            context.Report(FeatureUsage.ForTargetFramework(framework));
    }

    private static NuGetFramework? InferFramework(IAssembly assembly)
    {
        var tfm = assembly.GetTargetFrameworkMoniker();
        return !string.IsNullOrEmpty(tfm)
                ? NuGetFramework.Parse(tfm)
                : InferFrameworkFromReferences(assembly);
    }

    private static NuGetFramework? InferFrameworkFromReferences(IAssembly assembly)
    {
        foreach (var assemblyReference in assembly.AssemblyReferences)
        {
            var name = assemblyReference.Name.Value;
            var major = assemblyReference.Version.Major;
            var minor = assemblyReference.Version.Minor;
            var build = assemblyReference.Version.Build;

            if (string.Equals(name, "System.Runtime", StringComparison.OrdinalIgnoreCase))
            {
                if (major >= 5)
                    return NuGetFramework.Parse($"netcoreapp{major}.{minor}");

                switch (major, minor, build)
                {
                    case (4, 1, _):
                        return NuGetFramework.Parse("netcoreapp1.0");
                    case (4, 2, 1):
                        return NuGetFramework.Parse("netcoreapp2.1");
                    case (4, 2, 2):
                        return NuGetFramework.Parse("netcoreapp3.1");
                    case (4, 2, _):
                        return NuGetFramework.Parse("netcoreapp2.0");
                    case (4, 0, 10):
                        return NuGetFramework.Parse("netstandard1.2");
                    case (4, 0, 20):
                        return NuGetFramework.Parse("netstandard1.3");
                    case (4, 0, _):
                        return NuGetFramework.Parse("netstandard1.0");
                }
            }

            if (string.Equals(name, "netstandard", StringComparison.OrdinalIgnoreCase))
                return NuGetFramework.Parse($"netstandard{major}.{minor}");

            if (string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(assemblyReference.GetPublicKeyToken(), "b77a5c561934e089"))
            {
                switch (major, minor, build)
                {
                    case (1, 0, 3300):
                        return NuGetFramework.Parse("net1.0");
                    case (1, 0, 5000):
                        return NuGetFramework.Parse("net2.0");
                    case (2, _, _):
                        return NuGetFramework.Parse("net2.0");
                    case (4, _, _):
                        return NuGetFramework.Parse("net4.0");
                }
            }
        }

        return null;
    }
}