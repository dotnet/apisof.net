public static class DumpPackDiagnostics
{
    private static readonly List<ErrorContent> s_messages = [];

    public static void Report(string severity, string message)
    {
        s_messages.Add(new ErrorContent
        {
            Severity = severity,
            Error = message
        });
    }

    public static List<ErrorContent> Drain()
    {
        var result = s_messages.ToList();
        s_messages.Clear();
        return result;
    }
}