using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using NuGet.Frameworks;

namespace Terrajobst.UsageCrawling.Collectors;

public sealed class TargetFrameworkCollector : IncrementalUsageCollector
{
    public override int VersionIntroduced => 3;

    protected override void CollectFeatures(IAssembly assembly, Context context)
    {
        var tfm = assembly.GetTargetFrameworkMoniker();
        if (!string.IsNullOrEmpty(tfm))
        {
            var nugetFramework = NuGetFramework.Parse(tfm);
            context.Report(FeatureUsage.ForTargetFramework(nugetFramework));
            return;
        }

        foreach (var assemblyReference in assembly.AssemblyReferences)
        {
            var name = assemblyReference.Name.Value;
            var major = assemblyReference.Version.Major;
            var minor = assemblyReference.Version.Minor;
            var build = assemblyReference.Version.Build;

            if (string.Equals(name, "System.Runtime", StringComparison.OrdinalIgnoreCase))
            {
                if (major >= 5)
                {
                    context.Report(FeatureUsage.ForTargetFramework($"netcoreapp{major}.{minor}"));
                    return;
                }

                switch (major, minor, build)
                {
                    case (4, 1, _):
                        context.Report(FeatureUsage.ForTargetFramework("netcoreapp1.0"));
                        return;
                    case (4, 2, 1):
                        context.Report(FeatureUsage.ForTargetFramework("netcoreapp2.1"));
                        return;
                    case (4, 2, 2):
                        context.Report(FeatureUsage.ForTargetFramework("netcoreapp3.1"));
                        return;
                    case (4, 2, _):
                        context.Report(FeatureUsage.ForTargetFramework("netcoreapp2.0"));
                        return;
                    case (4, 0, 10):
                        context.Report(FeatureUsage.ForTargetFramework("netstandard1.2"));
                        return;
                    case (4, 0, 20):
                        context.Report(FeatureUsage.ForTargetFramework("netstandard1.3"));
                        return;
                    case (4, 0, _):
                        context.Report(FeatureUsage.ForTargetFramework("netstandard1.0"));
                        return;
                }
            }

            if (string.Equals(name, "netstandard", StringComparison.OrdinalIgnoreCase))
            {
                context.Report(FeatureUsage.ForTargetFramework($"netstandard{major}.{minor}"));
                return;
            }

            if (string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(assemblyReference.GetPublicKeyToken(), "b77a5c561934e089"))
            {
                switch (major, minor, build)
                {
                    case (1, 0, 3300):
                        context.Report(FeatureUsage.ForTargetFramework("net1.0"));
                        return;
                    case (1, 0, 5000):
                        context.Report(FeatureUsage.ForTargetFramework("net2.0"));
                        return;
                    case (2, _, _):
                        context.Report(FeatureUsage.ForTargetFramework("net2.0"));
                        return;
                    case (4, _, _):
                        context.Report(FeatureUsage.ForTargetFramework("net4.0"));
                        return;
                }
            }
        }
    }
}