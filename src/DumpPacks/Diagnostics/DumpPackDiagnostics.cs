using Terrajobst.ApiCatalog.PackManifest.Models;

public sealed class DumpPackDiagnostics
{
    private readonly List<ErrorContent> _messages = [];

    public void Report(ErrorSeverity severity, string message)
    {
        _messages.Add(new ErrorContent
        {
            Severity = severity,
            Error = message
        });
    }

    public List<ErrorContent> Drain()
    {
        var result = _messages.ToList();
        _messages.Clear();
        return result;
    }
}