namespace ApisOfDotNet.Services;

public sealed class CatalogJobInfo
{
    public static CatalogJobInfo Empty { get; } = new()
    {
        Date = DateTimeOffset.MinValue,
        Success = true,
        DetailsUrl = "https://github.com/dotnet/apisof.net/actions/workflows/gen-catalog.yml"
    };
    
    public required DateTimeOffset Date { get; set; }
    public required bool Success { get; set; }
    public required string DetailsUrl { get; set; }
}