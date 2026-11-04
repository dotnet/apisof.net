namespace Terrajobst.ApiCatalog.PackManifest.Models;

public class BuiltInPackManifest
{
    public string SdkVersion { get; set; } = string.Empty;
    public ICollection<FrameworkReferenceContent> FrameworkReferences { get; set; } = new List<FrameworkReferenceContent>();
    public ICollection<PlatformVersion> PlatformVersions { get; set; } = new List<PlatformVersion>();

}

public class FrameworkReferenceContent
{
    public string TargetFramework { get; set; } = string.Empty;
    // Split out at dump time from the already-parsed NuGetFramework so the
    // consumer never has to re-parse the short folder name.
    public string Platform { get; set; } = string.Empty;
    public string PlatformVersion { get; set; } = string.Empty;
    public ICollection<FrameworkReferencePack> Packs { get; set; } = new List<FrameworkReferencePack>();
}

public class FrameworkReferencePack
{
    public string PackName { get; set; } = string.Empty;
    public string PackVersion { get; set; } = string.Empty;
}