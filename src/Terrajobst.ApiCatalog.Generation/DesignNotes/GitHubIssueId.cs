namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

public readonly struct GitHubIssueId : IEquatable<GitHubIssueId>
{
    public GitHubIssueId(string org, string repo, int number)
    {
        ThrowIfNullOrEmpty(org);
        ThrowIfNullOrEmpty(repo);
        ThrowIfNegativeOrZero(number);

        Org = org;
        Repo = repo;
        Number = number;
    }

    public string Org { get; }

    public string Repo { get; }

    public int Number { get; }

    public string GetUrl(string? commentId = null)
    {
        var issueUrl = $"https://github.com/{Org}/{Repo}/issues/{Number}";
        return commentId is null ? issueUrl : $"{issueUrl}#issuecomment-{commentId}";
    }

    public bool Equals(GitHubIssueId other)
    {
        return string.Equals(Org, other.Org, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Repo, other.Repo, StringComparison.OrdinalIgnoreCase) &&
               Number == other.Number;
    }

    public override bool Equals(object? obj)
    {
        return obj is GitHubIssueId other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Org, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Repo, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Number);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(GitHubIssueId left, GitHubIssueId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GitHubIssueId left, GitHubIssueId right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Org}/{Repo}#{Number}";
    }
}