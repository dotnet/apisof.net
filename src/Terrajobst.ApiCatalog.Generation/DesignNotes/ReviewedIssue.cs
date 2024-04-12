namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

public sealed class ReviewedIssue
{
    internal ReviewedIssue(string decision, GitHubIssueId id, string? commentId, string? videoUrl)
    {
        ThrowIfNullOrEmpty(decision);

        if (string.IsNullOrEmpty(commentId))
            commentId = null;

        if (string.IsNullOrEmpty(videoUrl))
            commentId = null;

        Decision = decision;
        Id = id;
        CommentId = commentId;
        VideoUrl = videoUrl;
    }

    public string Decision { get; }
    public GitHubIssueId Id { get; }
    public string? CommentId { get; }
    public string? VideoUrl { get; }
    public List<string> Body { get; set; } = new();
    public List<Range> SourceBlocks { get; set; } = new();
    public List<ReviewedApi> Apis { get; set; } = new();
}