namespace Terrajobst.ApiCatalog.PackManifest.Models;

public class DumpPackManifest
{
    public ICollection<BuiltInPackManifest> BuiltInPackManifests { get; set; } = new List<BuiltInPackManifest>();
    public ICollection<WorkloadPackManifest> WorkloadPackManifests { get; set; } = new List<WorkloadPackManifest>();
    public ICollection<ErrorContent> Errors { get; set; } = new List<ErrorContent>();
}

public class ErrorContent
{
    public ErrorSeverity Severity { get; set; }
    public string Error { get; set; } = string.Empty;
}

public enum ErrorSeverity
{
    Error,
    Warning
}