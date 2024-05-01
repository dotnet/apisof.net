using System.Diagnostics;

using LibGit2Sharp;

using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.ActionsRunner;
using Terrajobst.ApiCatalog.Generation.DesignNotes;

namespace GenDesignNotes;

internal sealed class Main : IConsoleMain
{
    private readonly ApisOfDotNetPathProvider _pathProvider;
    private readonly ApisOfDotNetStore _store;
    private readonly ApisOfDotNetWebHook _webHook;
    private readonly GitHubActionsSummaryTable _summaryTable;

    public Main(ApisOfDotNetPathProvider pathProvider,
                ApisOfDotNetStore store,
                ApisOfDotNetWebHook webHook,
                GitHubActionsSummaryTable summaryTable)
    {
        _pathProvider = pathProvider;
        _store = store;
        _webHook = webHook;
        _summaryTable = summaryTable;
    }

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var catalogModelPath = _pathProvider.CatalogModelPath;
        var reviewRepoPath = _pathProvider.ReviewRepoPath;
        var designNotesPath = _pathProvider.DesignNotesPath;

        var stopwatch = Stopwatch.StartNew();

        await DownloadCatalog(catalogModelPath);
        await DownloadApiReviewsRepoAsync(reviewRepoPath);
        await GenerateDesignNotesAsync(reviewRepoPath, catalogModelPath, designNotesPath);
        await _store.UploadDesignNotesAsync(designNotesPath);

        await _webHook.InvokeAsync(ApisOfDotNetWebHookSubject.DesignNotes);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private async Task DownloadCatalog(string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
        {
            Console.WriteLine($"Skipping download of {Path.GetFileName(catalogModelPath)}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(catalogModelPath)!);
        await _store.DownloadToAsync("catalog", "apicatalog.dat", catalogModelPath);
    }

    private Task DownloadApiReviewsRepoAsync(string reviewRepoPath)
    {
        if (Directory.Exists(reviewRepoPath))
        {
            Console.WriteLine($"Skipping download of {Path.GetFileName(reviewRepoPath)}");
            return Task.CompletedTask;
        }

        var url = "https://github.com/dotnet/apireviews";
        Console.WriteLine($"Cloning {url}...");
        Repository.Clone(url, reviewRepoPath);
        return Task.CompletedTask;
    }

    private async Task GenerateDesignNotesAsync(string reviewRepoPath, string catalogModelPath, string designNotesPath)
    {
        if (File.Exists(designNotesPath))
        {
            Console.WriteLine($"Skipping generation of {Path.GetFileName(designNotesPath)}");
            return;
        }

        Console.WriteLine("Generating design notes...");
        var reviewDatabase = ReviewDatabase.Load(reviewRepoPath);
        var catalog = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var database = DesignNoteBuilder.Build(reviewDatabase, catalog);
        database.Save(designNotesPath);

        var designNotesName = Path.GetFileName(designNotesPath);
        var designNotesSize = new FileInfo(designNotesPath).Length;
        var apiCount = database.GetApiCount();
        var noteCount = database.GetNoteCount();

        _summaryTable.AppendNumber("#APIs with notes", apiCount);
        _summaryTable.AppendNumber("#Notes", noteCount);
        _summaryTable.AppendBytes(designNotesName, designNotesSize);
    }
}