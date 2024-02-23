using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.Versioning;

public sealed class KnownFrameworkReference
{
    public KnownFrameworkReference(string frameworkName,
                                   NuGetFramework targetFramework,
                                   string targetingPackName,
                                   NuGetVersion targetingPackVersion)
    {
        FrameworkName = frameworkName;
        TargetFramework = targetFramework;
        TargetingPackName = targetingPackName;
        TargetingPackVersion = targetingPackVersion;
    }

    public string FrameworkName { get; }
    public NuGetFramework TargetFramework { get; }
    public string TargetingPackName { get; }
    public NuGetVersion TargetingPackVersion { get; }

    public static IReadOnlyList<KnownFrameworkReference> Load(string path)
    {
        var result = new List<KnownFrameworkReference>();

        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file);
            var hasMSBuildExtension = string.Equals(extension, ".props", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(extension, ".targets", StringComparison.OrdinalIgnoreCase);

            if (!hasMSBuildExtension)
                continue;

            try
            {
                var document = XDocument.Load(file);
                var elements = document.Descendants().Where(e => e.Name.LocalName == "KnownFrameworkReference");

                foreach (var e in elements)
                {
                    var frameworkName = e.Attribute("Include")?.Value;
                    if (string.IsNullOrEmpty(frameworkName))
                        continue;

                    var targetFrameworkText = e.Attribute("TargetFramework")!.Value;
                    var targetingPackName = e.Attribute("TargetingPackName")!.Value;
                    var targetingPackVersionText = e.Attribute("TargetingPackVersion")!.Value;

                    var targetFramework = NuGetFramework.Parse(targetFrameworkText);
                    var targetingPackVersion = NuGetVersion.Parse(targetingPackVersionText);

                    // Strip platform and version because we don't care here.
                    targetFramework = new NuGetFramework(targetFramework.Framework, targetFramework.Version);

                    var reference = new KnownFrameworkReference(frameworkName, targetFramework, targetingPackName, targetingPackVersion);
                    result.Add(reference);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: Can't read '{file}': {ex.Message}");
            }
        }

        return result.ToArray();
    }
}