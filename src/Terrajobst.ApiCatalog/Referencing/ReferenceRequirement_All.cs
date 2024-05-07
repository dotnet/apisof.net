using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Terrajobst.ApiCatalog;

partial class ReferenceRequirement
{
    public static SdkRequirement Sdk(string sdkName)
    {
        ThrowIfNullOrEmpty(sdkName);

        return new SdkRequirement(sdkName);
    }

    public static WorkloadRequirement Workload(string workloadName)
    {
        ThrowIfNullOrEmpty(workloadName);

        return new WorkloadRequirement(workloadName);
    }

    private static TargetPlatformRequirement TargetPlatform(string platformName)
    {
        ThrowIfNullOrEmpty(platformName);

        return new TargetPlatformRequirement(platformName);
    }

    public static FrameworkReferenceRequirement FrameworkReference(string frameworkName)
    {
        ThrowIfNullOrEmpty(frameworkName);

        return new FrameworkReferenceRequirement(frameworkName);
    }

    public static AssemblyReferenceRequirement AssemblyReference(string assemblyName)
    {
        ThrowIfNullOrEmpty(assemblyName);

        return new AssemblyReferenceRequirement(assemblyName);
    }

    public static PackageReferenceRequirement PackageReference(string packageName)
    {
        ThrowIfNullOrEmpty(packageName);

        return new PackageReferenceRequirement(packageName);
    }

    public static PropertyRequirement Property(string propertyName)
    {
        ThrowIfNullOrEmpty(propertyName);

        return new PropertyRequirement(propertyName);
    }

    public static PropertyRequirement UseWindowsForms() => Property("UseWindowsForms");

    public static PropertyRequirement UseWPF() => Property("UseWPF");

    public static PropertyRequirement UseMaui() => Property("UseMaui");

    public static PropertyRequirement IsAspireHost() => Property("IsAspireHost");

    public static PlatformRequirement PlatformRequirement(string platformName)
    {
        ThrowIfNullOrEmpty(platformName);

        return new PlatformRequirement(platformName);
    }

    public static AndReferenceRequirement And(params ReferenceRequirement?[] requirements)
    {
        ThrowIfNull(requirements);

        var nonNullRequirements = requirements.Where(r => r is not null).Select(r => r!).ToArray();
        return new AndReferenceRequirement(nonNullRequirements);
    }

    public static OrReferenceRequirement Or(params ReferenceRequirement?[] requirements)
    {
        ThrowIfNull(requirements);

        var nonNullRequirements = requirements.Where(r => r is not null).Select(r => r!).ToArray();
        return new OrReferenceRequirement(nonNullRequirements);
    }
}

public sealed class AndReferenceRequirement : ReferenceRequirement
{
    public AndReferenceRequirement(IReadOnlyList<ReferenceRequirement> requirements)
    {
        ThrowIfNull(requirements);

        Requirements = requirements;
    }

    public IReadOnlyList<ReferenceRequirement> Requirements { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine("Requires all:");
        foreach (var r in Requirements)
        {
            var lines = r.ToString().Split(Environment.NewLine);
            var isFirst = true;

            foreach (var line in lines)
            {
                if (isFirst)
                {
                    writer.WriteLine("* " + line);
                    isFirst = false;
                    writer.Indent++;
                }
                else
                {
                    writer.WriteLine(line);
                }
            }

            if (!isFirst)
                writer.Indent--;
        }
    }
}

public sealed class OrReferenceRequirement : ReferenceRequirement
{
    public OrReferenceRequirement(IReadOnlyList<ReferenceRequirement> requirements)
    {
        ThrowIfNull(requirements);

        Requirements = requirements;
    }

    public IReadOnlyList<ReferenceRequirement> Requirements { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine("Requires any:");
        foreach (var r in Requirements)
        {
            var lines = r.ToString().Split(Environment.NewLine);
            var isFirst = true;

            foreach (var line in lines)
            {
                if (isFirst)
                {
                    writer.WriteLine("* " + line);
                    isFirst = false;
                    writer.Indent++;
                }
                else
                {
                    writer.WriteLine(line);
                }
            }

            if (!isFirst)
                writer.Indent--;
        }
    }
}

public sealed class SdkRequirement : ReferenceRequirement
{
    public SdkRequirement(string sdkName)
    {
        ThrowIfNullOrEmpty(sdkName);

        SdkName = sdkName;
    }

    public string SdkName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine($"Your project needs to use SDK `{SdkName}`.");
    }
}

public sealed class WorkloadRequirement : ReferenceRequirement
{
    public WorkloadRequirement(string workloadName)
    {
        ThrowIfNullOrEmpty(workloadName);

        WorkloadName = workloadName;
    }

    public string WorkloadName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine($"You need to have the workload `{WorkloadName}` installed.");
    }
}

public sealed class TargetPlatformRequirement : ReferenceRequirement
{
    public TargetPlatformRequirement(string platformName)
    {
        ThrowIfNullOrEmpty(platformName);

        PlatformName = platformName;
    }

    public string PlatformName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        writer.WriteLine($"The `<TargetFramework>` property needs to include `{PlatformName}`.");
    }
}

public sealed class FrameworkReferenceRequirement : ReferenceRequirement
{
    public FrameworkReferenceRequirement(string frameworkName)
    {
        ThrowIfNullOrEmpty(frameworkName);

        FrameworkName = frameworkName;
    }

    public string FrameworkName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine($"Your project needs a framework reference to `{FrameworkName}`.");
    }
}

public sealed class PackageReferenceRequirement : ReferenceRequirement
{
    public PackageReferenceRequirement(string packName)
    {
        ThrowIfNullOrEmpty(packName);

        PackageName = packName;
    }

    public string PackageName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine($"Your project needs a package reference to `{PackageName}`.");
    }
}

public sealed class AssemblyReferenceRequirement : ReferenceRequirement
{
    public AssemblyReferenceRequirement(string assemblyName)
    {
        ThrowIfNullOrEmpty(assemblyName);

        AssemblyName = assemblyName;
    }

    public string AssemblyName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine($"Your project needs an assembly reference to `{AssemblyName}`.");
    }
}

public sealed class PropertyRequirement : ReferenceRequirement
{
    public PropertyRequirement(string usePropertyName)
    {
        ThrowIfNullOrEmpty(usePropertyName);

        UsePropertyName = usePropertyName;
    }

    public string UsePropertyName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        writer.WriteLine($"Your project needs to set `<{UsePropertyName}>True</{UsePropertyName}>`.");
    }
}

public sealed class PlatformRequirement : ReferenceRequirement
{
    public PlatformRequirement(string platformName)
    {
        ThrowIfNullOrEmpty(platformName);

        PlatformName = platformName;
    }

    public string PlatformName { get; }

    public override void WriteTo(IndentedTextWriter writer)
    {
        ThrowIfNull(writer);

        var formattedPlatform = PlatformAnnotationEntry.FormatPlatform(PlatformName);
        writer.WriteLine($"You need to build and run on {formattedPlatform}.");
    }
}