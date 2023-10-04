using System.IO;

using Terrajobst.ApiCatalog;

namespace NetUpgradePlanner.Services;

internal sealed class CatalogService
{
    private readonly ProgressService _progressService;
    private readonly string _catalogFilePath;
    private ApiCatalogModel? _catalog;

    public CatalogService(ProgressService progressService)
    {
        _progressService = progressService;
        _catalogFilePath = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "catalog.dat");
    }

    public async Task<ApiCatalogModel> GetAsync()
    {
        if (_catalog is null)
        {
            var forceDownload = IsNotCachedOrExpired();
            _catalog = await LoadAsync(forceDownload);
        }

        return _catalog;
    }

    public async Task UpdateAsync()
    {
        _catalog = await LoadAsync(forceDownload: true);
    }

    private async Task<ApiCatalogModel> LoadAsync(bool forceDownload)
    {
        if (!File.Exists(_catalogFilePath) || forceDownload)
            await _progressService.Run(_ => ApiCatalogModel.DownloadFromWebAsync(_catalogFilePath), "Downloading catalog...");

        return await _progressService.Run(_ => ApiCatalogModel.LoadAsync(_catalogFilePath), "Reading catalog...");
    }

    private bool IsNotCachedOrExpired()
    {
        if (!File.Exists(_catalogFilePath))
            return true;

        var age = DateTime.Now - File.GetLastWriteTime(_catalogFilePath);
        return age.TotalHours >= 24;
    }
}
