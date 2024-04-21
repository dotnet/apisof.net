namespace GenUsageNuGet.Infra;

public abstract class CrawlerStore
{
    protected const string ApiCatalogName = "apicatalog.dat";
    protected const string DatabaseName = "usages-nuget.db";
    protected const string UsagesName = "usages-nuget.tsv";

    public abstract Task DownloadApiCatalogAsync(string fileName);
    public abstract Task<bool> DownloadDatabaseAsync(string fileName);
    public abstract Task UploadDatabaseAsync(string fileName);
    public abstract Task UploadResultsAsync(string fileName);
}