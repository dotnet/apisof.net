namespace Terrajobst.ApiCatalog.ActionsRunner;

public class GitHubActionsLog
{
    private readonly GitHubActionsEnvironment _environment;

    public GitHubActionsLog(GitHubActionsEnvironment environment)
    {
        ThrowIfNull(environment);

        _environment = environment;
    }

    public Group BeginGroup(string title)
    {
        var logMessage = _environment.IsRunningInActions
            ? $"::group::{title}"
            : title;
        Console.WriteLine(logMessage);
        return new Group(this);
    }

    public void EndGroup()
    {
        if (_environment.IsRunningInActions)
            Console.WriteLine("::endgroup::");
    }

    public void AppendSummary(string markdown)
    {
        if (string.IsNullOrEmpty(_environment.StepSummary))
            return;
        
        using var writer = File.AppendText(markdown);
        writer.WriteLine(markdown);
    }
    
    public readonly struct Group : IDisposable
    {
        private readonly GitHubActionsLog _log;

        internal Group(GitHubActionsLog log)
        {
            _log = log;
        }
        
        public void Dispose()
        {
            _log.EndGroup();
        }
    }
}