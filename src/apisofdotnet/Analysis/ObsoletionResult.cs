internal readonly struct ObsoletionResult
{
    public ObsoletionResult(string message, string url)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(url);

        Message = message;
        Url = url;
    }

    public string Message { get; }

    public string Url { get; }
}