namespace Terrajobst.ApiCatalog.PackManifest.Models;

public class PlatformVersion
{
    public string Platform { get; set; } = string.Empty;
    public ICollection<string> Versions { get; set; } = new List<string>();
}