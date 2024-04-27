namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class ApisOfDotNetPathProvider
{
    public ApisOfDotNetPathProvider()
    {
        var environmentPath = Environment.GetEnvironmentVariable("APISOFDOTNET_INDEX_PATH");
        var defaultPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Catalog");
        RootPath = environmentPath ?? defaultPath;
        CatalogModelPath = Path.Join(RootPath, "apicatalog.dat");
        ReviewRepoPath = Path.Join(RootPath, "apireviews");
        DesignNotesPath = Path.Join(RootPath, "designNotes.dat");
    }

    public string RootPath { get; }
    public string DesignNotesPath { get; }
    public string ReviewRepoPath { get; }
    public string CatalogModelPath { get; }
}