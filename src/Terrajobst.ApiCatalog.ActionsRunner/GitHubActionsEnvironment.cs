namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class GitHubActionsEnvironment
{
    public string? GitHubServerUrl { get; } = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");

    public string? GitHubRepository { get; } = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");

    public string? GitHubRunId { get; } = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");

    public string? GitHubActionsUrl()
    {
        var serverUrl = GitHubServerUrl;
        var repository = GitHubRepository;
        var runId = GitHubRunId;
        return serverUrl is null || repository is null || runId is null
            ? null
            : $"{serverUrl}/{repository}/actions/runs/{runId}";
    }
}