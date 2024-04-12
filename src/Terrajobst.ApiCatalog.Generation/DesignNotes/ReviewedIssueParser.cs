using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

internal sealed partial class ReviewedIssueParser
{
    [GeneratedRegex(@"https\://github\.com/(?<Org>[^/]+)/(?<Repo>[^/]+)/[^/]+/(?<Number>[^/#]+)(#issuecomment-(?<CommentId>[0-9]+))?")]
    public static partial Regex GetUrlRegex();

    [GeneratedRegex(@"\*\*(?<Decision>[^\*]+)\*\*\s*\|(\r|\n|\s)*\[[^\]]*\]\((?<Url>[^)]*)\)(\s*\|(\r|\n|\s)*\[Video\]\((?<VideoUrl>[^)]*)\))?")]
    public static partial Regex GetStatusRegex();

    private readonly Regex _statusRegex = GetStatusRegex();
    private readonly Regex _urlRegex = GetUrlRegex();

    public bool TryParse(string line, [MaybeNullWhen(false)] out ReviewedIssue result)
    {
        var statusMatch = _statusRegex.Match(line);

        if (statusMatch.Success)
        {
            var decision = statusMatch.Groups["Decision"].Value;
            var url = statusMatch.Groups["Url"].Value;
            var videoUrl = statusMatch.Groups["VideoUrl"].Value;

            var urlMatch = _urlRegex.Match(url);
            if (urlMatch.Success)
            {
                var org = urlMatch.Groups["Org"].Value;
                var repo = urlMatch.Groups["Repo"].Value;
                var numberText = urlMatch.Groups["Number"].Value;
                var commentId = urlMatch.Groups["CommentId"].Value;
                if (int.TryParse(numberText, out var number))
                {
                    var id = new GitHubIssueId(org, repo, number);
                    result = new ReviewedIssue(decision, id, commentId, videoUrl);
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}