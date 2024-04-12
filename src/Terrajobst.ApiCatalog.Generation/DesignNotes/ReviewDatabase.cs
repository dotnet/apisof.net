using System.Collections.Immutable;
using System.Diagnostics;
using LibGit2Sharp;

namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

public sealed class ReviewDatabase
{
    private ReviewDatabase(IEnumerable<ReviewSession> sessions)
    {
        Sessions = sessions.ToImmutableArray();
        ThrowIfNull(sessions);
    }

    public ImmutableArray<ReviewSession> Sessions { get; }

    public static ReviewDatabase Load(string path)
    {
        ThrowIfNullOrEmpty(path);

        var repositoryPath = Repository.Discover(path);
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("The path needs to be a Git repository.", nameof(path));

        var repository = new Repository(repositoryPath);

        var parser = new ReviewedIssueParser();
        var result = new List<ReviewSession>();

        var files = Directory.EnumerateFiles(path, "README.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(path, file).Replace(Path.DirectorySeparatorChar, '/');
            var commit = repository.Commits.FirstOrDefault(c => AffectsFile(c, relativePath));

            if (commit?.Committer.When is null)
                Debugger.Break();

            var dateTime = commit?.Committer.When ?? DateTimeOffset.Now;

            var lines = File.ReadAllLines(file);
            var session = ParseSession(dateTime, relativePath, lines, parser);
            result.Add(session);
        }

        return new ReviewDatabase(result);
    }

    private static bool AffectsFile(Commit commit, string relativePath)
    {
        if (commit.Parents.Count() != 1)
            return false;

        var parent = commit.Parents.Single();
        var commitTree = commit.Tree[relativePath];
        var parentTree = parent.Tree[relativePath];
        return commitTree is not null &&
               (parentTree is null || commitTree.Target.Id != parentTree.Target.Id);
    }

    private static ReviewSession ParseSession(DateTimeOffset dateTime, string relativePath, string[] lines, ReviewedIssueParser parser)
    {
        var session = new ReviewSession(relativePath, dateTime);

        ReviewedIssue? current = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("##"))
            {
                if (current is not null)
                {
                    session.Issues.Add(current);
                    current = null;
                }
            }
            else if (parser.TryParse(line, out var i))
            {
                current = i;
            }
            else if (current is not null)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 0 || current.Body.Count > 0)
                    current.Body.Add(trimmedLine);
            }
        }

        if (current is not null)
            session.Issues.Add(current);

        foreach (var issue in session.Issues)
        {
            RecordSourceBlocks(issue);
            RecordApis(issue);
        }

        return session;
    }

    private static void RecordSourceBlocks(ReviewedIssue issue)
    {
        var start = -1;

        for (var i = 0; i < issue.Body.Count; i++)
        {
            var line = issue.Body[i];
            if (line.StartsWith("```"))
            {
                if (start == -1)
                {
                    start = i;
                }
                else
                {
                    var end = i;
                    var range = new Range(start + 1, end - 1);
                    issue.SourceBlocks.Add(range);
                    start = -1;
                }
            }
        }

        if (start != -1)
        {
            var range = new Range(start + 1, issue.Body.Count);
            issue.SourceBlocks.Add(range);
        }
    }

    private static void RecordApis(ReviewedIssue issue)
    {
        foreach (var blockRange in issue.SourceBlocks)
        {
            var source = string.Join(Environment.NewLine, issue.Body[blockRange]);
            var apiParser = new ReviewedApiParser(issue.Apis);
            apiParser.Parse(source);
        }
    }
}