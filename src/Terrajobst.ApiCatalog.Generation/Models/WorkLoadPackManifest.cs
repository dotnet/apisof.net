public class WorkLoadPackManifest
{
    public string DotNetVersion { get; set; } = string.Empty;
    public ICollection<WorkLoadPackContent> Packs { get; set; } = new List<WorkLoadPackContent>();
    public ICollection<PlatformVersion> PlatformVersions { get; set; } = new List<PlatformVersion>();
}
public class WorkLoadPackContent
{
    public string PackName { get; set; } = string.Empty;
    public string PackVersion { get; set; } = string.Empty;
    public string PackKind { get; set; } = string.Empty;
    public ICollection<string> WorkloadNames { get; set; } = new List<string>();
}