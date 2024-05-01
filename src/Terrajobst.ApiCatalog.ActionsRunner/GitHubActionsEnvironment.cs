namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class GitHubActionsEnvironment
{
    public GitHubActionsEnvironment()
    {
        RunUrl =
            ServerUrl is null || Repository is null || RunId is null
                ? null
                : $"{ServerUrl}/{Repository}/actions/runs/{RunId}";
    }

    public bool IsRunningInActions => RunId is not null;

    public string? ServerUrl { get; } = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");

    public string? Repository { get; } = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");

    public string? RunId { get; } = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");

    public string? StepSummary { get; } = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

    public string? RunUrl { get; }
}