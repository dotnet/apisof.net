namespace Terrajobst.ApiCatalog.PackManifest.Models;

public class WorkloadPackManifest
{
    public string DotNetVersion { get; set; } = string.Empty;
    public ICollection<WorkloadPackContent> Packs { get; set; } = new List<WorkloadPackContent>();
    public ICollection<PlatformVersion> PlatformVersions { get; set; } = new List<PlatformVersion>();
}
public class WorkloadPackContent
{
    public string PackName { get; set; } = string.Empty;
    public string PackVersion { get; set; } = string.Empty;
    public string PackKind { get; set; } = string.Empty;
    // Platforms this pack targets, resolved from the SDK's own MSBuild metadata
    // at dump time. Empty for platform-agnostic (Library) packs.
    public ICollection<string> Platforms { get; set; } = new List<string>();
    public ICollection<string> WorkloadNames { get; set; } = new List<string>();
}