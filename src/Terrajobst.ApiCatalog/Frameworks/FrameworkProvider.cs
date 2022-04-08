namespace Terrajobst.ApiCatalog;

/// <summary>
/// This is used to enumerate the frameworks that should be indexed.
/// </summary>
public abstract class FrameworkProvider
{
    public abstract IEnumerable<(string FrameworkName, string[] Paths)> Resolve();
}