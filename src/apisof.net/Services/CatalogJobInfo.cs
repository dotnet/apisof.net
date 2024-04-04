namespace ApisOfDotNet.Services;

public sealed class CatalogJobInfo
{
    public static CatalogJobInfo Empty { get; } = new();

    private CatalogJobInfo()
    {
        Date = DateTimeOffset.MinValue;
        Success = true;
        DetailsUrl = "https://github.com/dotnet/apisof.net/actions/workflows/gen-catalog.yml";
    }

    public CatalogJobInfo(DateTimeOffset date, bool success, string detailsUrl)
    {
        Date = date;
        Success = success;
        DetailsUrl = detailsUrl;
    }

    public DateTimeOffset Date { get; }
    public bool Success { get; }
    public string DetailsUrl { get; }
}