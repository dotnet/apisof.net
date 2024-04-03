using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ApisOfDotNet.Shared;

public partial class MainLayout
{
    private ElementReference _bodyDiv;
    private ApiSearch _apiSearch = null!; // Initialized in Razor
    public CatalogJobInfo CatalogJobInfo => CatalogService.JobInfo;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _bodyDiv.FocusAsync();
    }

    private void KeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "/")
        {
            if (_apiSearch.IsOpen)
                _apiSearch.Close();
            else
                _apiSearch.Open();
        }
        else if (e.Key == "Escape")
        {
            _apiSearch.Close();
        }
    }
}