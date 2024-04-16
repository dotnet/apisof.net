using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;

namespace ApisOfDotNet.Pages;

public partial class Version
{
    [Inject]
    public required VersionService VersionService { get; set; }

    public string CommitTitle { get; set; } = "";

    public string CommitUrl { get; set; } = "";

    public string CommitHash { get; set; } = "";

    public string CommitHashShort { get; set; } = "";

    public int CatalogVersion { get; set; }

    protected override async Task OnInitializedAsync()
    {
        (CommitTitle, CommitUrl, CommitHash) = await VersionService.GetCommitAsync();
        CommitHashShort = CommitHash.Substring(0, int.Min(CommitHash.Length, 7));
        
        CatalogVersion = CatalogService.Catalog.FormatVersion;
    }
}