public class DumpPackManifest
{
    public ICollection<BuiltInPackManifest> BuiltInPackManifests { get; set; } = new List<BuiltInPackManifest>();
    public ICollection<WorkLoadPackManifest> WorkLoadPackManifests { get; set; } = new List<WorkLoadPackManifest>();
    public ICollection<ErrorContent> Errors { get; set; } = new List<ErrorContent>();
}

public class ErrorContent
{
    public string Severity { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}