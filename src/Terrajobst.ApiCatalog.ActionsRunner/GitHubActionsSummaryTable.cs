using Humanizer;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public class GitHubActionsSummaryTable
{
    private readonly GitHubActionsLog _actionsLog;
    private bool _headerWritten;

    public GitHubActionsSummaryTable(GitHubActionsLog actionsLog)
    {
        ThrowIfNull(actionsLog);

        _actionsLog = actionsLog;
        _actionsLog.AppendSummary("### Data");
        _actionsLog.AppendSummary("Hopefully this renders on GitHub.");
    }

    private void EnsureHeader()
    {
        // if (_headerWritten)
        //     return;
        //
        // _actionsLog.AppendSummary("| Metric | Value |");
        // _actionsLog.AppendSummary("|--------|-------|");
        // _headerWritten = true;
    }

    public void AppendNumber(string metricName, long value)
    {
        // EnsureHeader();
        // _actionsLog.AppendSummary($"| {metricName} | {value:N0} |");
    }

    public void AppendBytes(string metricName, long value)
    {
        // EnsureHeader();
        // _actionsLog.AppendSummary($"| {metricName} | {value.Bytes()} |");
    }
}