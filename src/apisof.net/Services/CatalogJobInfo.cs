namespace ApisOfDotNet.Services;

public sealed class CatalogJobInfo
{
    public required DateTimeOffset Date { get; set; }
    public required bool Success { get; set; }
    public required string DetailsUrl { get; set; }
}