using System.Collections.Frozen;
using System.Collections.Immutable;
using Terrajobst.ApiCatalog.DesignNotes;

namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

public sealed class DesignNoteBuilder
{
    private readonly ReviewDatabase _reviewsDatabase;
    private readonly ApiCatalogModel _catalog;
    private readonly Dictionary<GitHubIssueId, List<DesignNote>> _designNotesByIssue = new();
    private readonly Dictionary<int, HashSet<DesignNote>> _designNotesByApiId = new();
    private readonly Dictionary<int, HashSet<GitHubIssueId>> _issuesByApiId = new();

    private DesignNoteBuilder(ReviewDatabase reviewsDatabase, ApiCatalogModel catalog)
    {
        _reviewsDatabase = reviewsDatabase;
        _catalog = catalog;
    }

    private DesignNoteDatabase Build()
    {
        var apiResolver = new ApiResolver(_catalog);

        foreach (var session in _reviewsDatabase.Sessions)
        foreach (var issue in session.Issues)
        {
            var url = issue.Id.GetUrl(issue.CommentId);
            var urlText = issue.Id.ToString();
            var context = issue.Decision;
            var reviewLink = new DesignNote(session.DateTime, url, urlText, context);

            if (!_designNotesByIssue.TryGetValue(issue.Id, out var issueLinks))
            {
                issueLinks = new();
                _designNotesByIssue.Add(issue.Id, issueLinks);
            }

            issueLinks.Add(reviewLink);

            foreach (var apiReference in issue.Apis)
            {
                var api = apiResolver.Resolve(apiReference);
                if (api is null)
                    continue;

                if (!_issuesByApiId.TryGetValue(api.Value.Id, out var apiIssues))
                {
                    apiIssues = new();
                    _issuesByApiId.Add(api.Value.Id, apiIssues);
                }

                apiIssues.Add(issue.Id);

                if (!_designNotesByApiId.TryGetValue(api.Value.Id, out var apiLinks))
                {
                    apiLinks = new();
                    _designNotesByApiId.Add(api.Value.Id, apiLinks);
                }

                apiLinks.Add(reviewLink);
            }
        }

        foreach (var (apiId, apiLinks) in _designNotesByApiId)
        {
            var associatedIssueIds = _issuesByApiId[apiId];

            foreach (var issueId in associatedIssueIds)
            {
                var associatedLinks = _designNotesByIssue[issueId];
                apiLinks.UnionWith(associatedLinks);
            }
        }

        var mappings = _designNotesByApiId.Select(kv => KeyValuePair.Create(kv.Key, kv.Value.ToImmutableArray())).ToFrozenDictionary();
        return new DesignNoteDatabase(mappings);
    }

    public static DesignNoteDatabase Build(ReviewDatabase reviewsDatabase, ApiCatalogModel catalog)
    {
        ThrowIfNull(reviewsDatabase);
        ThrowIfNull(catalog);

        var builder = new DesignNoteBuilder(reviewsDatabase, catalog);
        return builder.Build();
    }
}