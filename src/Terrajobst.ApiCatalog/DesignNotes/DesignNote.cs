namespace Terrajobst.ApiCatalog.DesignNotes;

public sealed class DesignNote
{
    public DesignNote(DateTimeOffset date, string url, string urlText, string context)
    {
        Date = date;
        Url = url;
        UrlText = urlText;
        Context = context;
        ThrowIfNull(url);
        ThrowIfNull(urlText);
        ThrowIfNull(context);
    }

    public DateTimeOffset Date { get; }

    public string Url { get; }

    public string UrlText { get; }

    public string Context { get; }
}