
namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

public sealed class ReviewSession
{
    internal ReviewSession(string path, DateTimeOffset dateTime)
    {
        ThrowIfNullOrEmpty(path);

        Path = path;
        DateTime = dateTime;
        Issues = new();
    }

    public string Path { get; }

    public DateTimeOffset DateTime { get; }

    public List<ReviewedIssue> Issues { get; }
}
