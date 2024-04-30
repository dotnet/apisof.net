using System.Xml.Linq;

public sealed class SupportedTargetPlatformVersion
{
    public SupportedTargetPlatformVersion(string platform, Version version)
    {
        ThrowIfNullOrEmpty(platform);
        ThrowIfNull(version);
        Platform = platform;
        Version = version;
    }

    public string Platform { get; }

    public Version Version { get; }

    public static IReadOnlyList<SupportedTargetPlatformVersion> Load(string path)
    {
        var result = new List<SupportedTargetPlatformVersion>();

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
                const string ElementSuffix = "SdkSupportedTargetPlatformVersion";

                var document = XDocument.Load(file);
                var elements = document.Descendants().Where(e => e.Name.LocalName.EndsWith(ElementSuffix));

                foreach (var e in elements)
                {
                    var platformName = e.Name.LocalName.Replace(ElementSuffix, string.Empty);
                    var platformVersionText = e.Attribute("Include")?.Value;

                    if (string.IsNullOrEmpty(platformName) ||
                        string.IsNullOrEmpty(platformVersionText) ||
                        !Version.TryParse(platformVersionText, out var platformVersion))
                        continue;

                    var supportedPlatformVersion = new SupportedTargetPlatformVersion(platformName, platformVersion);
                    result.Add(supportedPlatformVersion);
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
