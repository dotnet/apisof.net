using System.Collections.Immutable;

using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

// Note: We could consider moving FrameworkDefinition from Terrajobst.ApiCatalog.Generation
//       into here and use to a table-driven approach, rather than hard coding.
//
//       This would probably be slightly more sense in the long run and would be easier to
//       version to.

partial class ReferenceRequirement
{
    public static ReferenceRequirement? Compute(ApiFrameworkAvailability availability)
    {
        var frameworkRequirement = GetFrameworkRequirement(availability);
        var packageRequirement = GetPackageRequirement(availability);

        // If the API is in box and is automatically referenced, then we want to return
        // null, to indicate that nothing needs to happen in order to reference the API.
        if (availability.IsInBox && frameworkRequirement is null)
            return null;

        // Otherwise, we want to combine both, even if the API is also in box.

        return Normalize(Or([frameworkRequirement, packageRequirement]));
    }

    private static ReferenceRequirement? GetFrameworkRequirement(ApiFrameworkAvailability availability)
    {
        if (!availability.FrameworkDeclarations.Any())
            return null;

        var catalog = availability.Declaration.Catalog;
        var assembly = availability.Declaration.Assembly;
        var framework = catalog.GetFramework(availability.Framework)!.Value;
        var (pack, profiles) = framework.GetPackAndProfiles(assembly);

        var isNetCoreApp3x = availability.Framework.Version.Major == 3 &&
                             string.Equals(availability.Framework.Framework, FrameworkConstants.FrameworkIdentifiers.NetCoreApp);

        var isNetFramework = string.Equals(availability.Framework.Framework, FrameworkConstants.FrameworkIdentifiers.Net);

        if (isNetFramework)
        {
            return GetNetFrameworkAssemblyRequirements(availability.Framework, assembly.Name);
        }
        else if (isNetCoreApp3x)
        {
            if (string.Equals(pack, "Microsoft.AspNetCore.App.Ref", StringComparison.OrdinalIgnoreCase))
            {
                return Or(Sdk("Microsoft.NET.Sdk.Web"), FrameworkReference("Microsoft.AspNetCore.App"));
            }
            else if (string.Equals(pack, "Microsoft.WindowsDesktop.App.Ref", StringComparison.OrdinalIgnoreCase))
            {
                return And(PlatformRequirement("windows"), GetWindowsDesktopProfileRequirements(profiles));
            }
        }
        else if (!string.IsNullOrEmpty(pack))
        {
            if (string.Equals(pack, "Microsoft.AspNetCore.App.Ref", StringComparison.OrdinalIgnoreCase))
            {
                return Or(Sdk("Microsoft.NET.Sdk.Web"), FrameworkReference("Microsoft.AspNetCore.App"));
            }
            else if (string.Equals(pack, "Microsoft.WindowsDesktop.App.Ref", StringComparison.OrdinalIgnoreCase))
            {
                return GetWindowsDesktopProfileRequirements(profiles);
            }
            if (string.Equals(pack, "Aspire.Hosting", StringComparison.OrdinalIgnoreCase))
            {
                return And(Workload("aspire"), IsAspireHost());
            }
            else if (pack.StartsWith("Microsoft.Maui.", StringComparison.OrdinalIgnoreCase))
            {
                return And(Workload("maui"), UseMaui());
            }
        }

        return null;

        static ReferenceRequirement? GetNetFrameworkAssemblyRequirements(NuGetFramework framework, string assemblyName)
        {
            var isAutoReferenced = NetFrameworkAutoReferencedAssemblies
                .Any(t => string.Equals(t.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase) && framework.Version >= t.Since);

            if (isAutoReferenced)
                return null;

            var desktopAutoReferenced = NetFrameworkDesktopAutoReferencedAssemblies
                .Where(t => string.Equals(t.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase) && framework.Version >= t.Since)
                .Cast<(string, Version, bool RequiresWindowsForms, bool RequiresWPF)?>()
                .SingleOrDefault();

            if (desktopAutoReferenced is not null)
            {
                var useWindowsForms = desktopAutoReferenced.Value.RequiresWindowsForms ? UseWindowsForms() : null;
                var useWpf = desktopAutoReferenced.Value.RequiresWPF ? UseWPF() : null;
                return And(useWindowsForms, useWpf);
            }

            return AssemblyReference(assemblyName);
        }

        static ReferenceRequirement GetWindowsDesktopProfileRequirements(ImmutableArray<string> profiles)
        {

            var winFormsProfile = profiles.Contains("WindowsForms", StringComparer.OrdinalIgnoreCase);
            var wpfProfile = profiles.Contains("WPF", StringComparer.OrdinalIgnoreCase);

            if (winFormsProfile && wpfProfile)
            {
                return Or(UseWindowsForms(), UseWPF());
            }
            else if (winFormsProfile)
            {
                return UseWindowsForms();
            }
            else if (wpfProfile)
            {
                return UseWPF();
            }
            else
            {
                // If a file has no profiles it means it's only available when the user sets both UseWindowsForms and UseWPF.
                // In practice this only applies to WindowsFormsIntegration.dll.
                return And(UseWindowsForms(), UseWPF());
            }
        }
    }

    private static ReferenceRequirement? GetPackageRequirement(ApiFrameworkAvailability availability)
    {
        if (!availability.PackageDeclarations.Any())
            return null;

        var api = availability.Declaration.Api;
        var packages = availability.PackageDeclarations
                                   .Select(p => p.Package.Name)
                                   .Distinct()
                                   .Order()
                                   .Select(PackageReference)
                                   .ToArray();
        return Or(packages);
    }

    private static readonly (string AssemblyName, Version Since)[] NetFrameworkAutoReferencedAssemblies =
    [
        ("mscorlib", new Version(0, 0)),
        ("System", new Version(0, 0)),
        ("System.Data", new Version(0, 0)),
        ("System.Drawing", new Version(0, 0)),
        ("System.Xml", new Version(0, 0)),

        ("System.Core", new Version(3, 5)),
        ("System.Runtime.Serialization", new Version(3, 5)),
        ("System.Xml.Linq", new Version(3, 5)),

        ("System.Numerics", new Version(4, 0)),

        ("System.IO.Compression.FileSystem", new Version(4, 5)),
    ];

    private static readonly (string AssemblyName, Version Since, bool RequiresUseWindowsForms, bool RequiresUseWPF)[] NetFrameworkDesktopAutoReferencedAssemblies =
    [
        // Windows Forms

        ("System.Windows.Forms", new Version(0, 0), true, false),

        ("WindowsFormsIntegration", new Version(3, 0), true, true),

        // WPF

        ("PresentationCore", new Version(3, 0), false, true),
        ("PresentationFramework", new Version(3, 0), false, true),
        ("WindowsBase", new Version(3, 0), false, true),

        ("System.Xaml", new Version(4, 0), false, true),
        ("UIAutomationClient", new Version(4, 0), false, true),
        ("UIAutomationClientSideProviders", new Version(4, 0), false, true),
        ("UIAutomationProvider", new Version(4, 0), false, true),
        ("UIAutomationTypes", new Version(4, 0), false, true),

        ("System.Windows.Controls.Ribbon", new Version(4, 5), false, true),
    ];
}
