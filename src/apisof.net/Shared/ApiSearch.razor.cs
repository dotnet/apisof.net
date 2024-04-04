using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Terrajobst.ApiCatalog;

using Toolbelt.Blazor.HotKeys2;

using Timer = System.Timers.Timer;

namespace ApisOfDotNet.Shared;

public sealed partial class ApiSearch : IDisposable
{
    private readonly Timer _debounceTimer;
    private HotKeysContext? _hotKeysContext;
    private ElementReference _inputElement;
    private string _searchText = "";

    public ApiSearch()
    {
        // Allow the user to type, and only perform the auto complete search after some milliseconds.
        // This limits the number of actual searches and let's the user type a bit before searching.
        _debounceTimer = new Timer
        {
            AutoReset = false,
            Interval = 150
        };
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;
    }
    
    public void Dispose()
    {
        _debounceTimer.Dispose();
        _hotKeysContext?.Dispose();
    }

    private string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            RestartDebounce();
        }
    }

    private ApiModel[] SearchResults { get; set; } = Array.Empty<ApiModel>();
    private ApiModel SelectedResult { get; set; }

    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required HotKeys HotKeys { get; set; }

    public bool IsOpen { get; private set; }

    protected override void OnInitialized()
    {
        _hotKeysContext = HotKeys.CreateContext()
                                 .Add(Key.Slash, Open);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen)
            await _inputElement.FocusAsync();
    }

    private async void OnDebounceTimerElapsed(object? sender, EventArgs args)
    {
        _debounceTimer.Stop();

        await UpdateSearchResults();
    }

    private void SearchTextKeyDown(KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.MetaKey || e.ShiftKey || e.AltKey)
            return;

        switch (e.Key)
        {
            case "ArrowUp":
                SelectedPrevious();
                break;
            case "ArrowDown":
                SelectedNext();
                break;
            case "Escape":
                Close();
                break;
            case "Enter":
                Accept();
                break;
        }
    }

    private void SelectedPrevious()
    {
        if (SelectedResult == default || SearchResults.Length == 0)
            SelectedResult = SearchResults.FirstOrDefault();
        else
        {
            var index = Array.IndexOf(SearchResults, SelectedResult);
            if (index > 0)
                index--;

            SelectedResult = SearchResults[index];
        }
    }

    private void SelectedNext()
    {
        if (SelectedResult == default || SearchResults.Length == 0)
            SelectedResult = SearchResults.FirstOrDefault();
        else
        {
            var index = Array.IndexOf(SearchResults, SelectedResult);
            if (index < SearchResults.Length - 1)
                index++;

            SelectedResult = SearchResults[index];
        }
    }

    private void SelectAndAccept(ApiModel selection)
    {
        SelectedResult = selection;
        Accept();
    }

    private void Accept()
    {
        if (SelectedResult != default)
        {
            NavigationManager.NavigateTo(Link.For(SelectedResult));
            Close();
        }
    }

    public void Open()
    {
        SearchText = "";
        SearchResults = Array.Empty<ApiModel>();
        SelectedResult = default;
        IsOpen = true;
        StateHasChanged();
    }

    private void Close()
    {
        IsOpen = false;
    }

    private void RestartDebounce()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async Task UpdateSearchResults()
    {
        if (SearchText is { Length: 0 })
            return;

        var results = CatalogService.Search(SearchText).ToArray();
        SearchResults = results;
        SelectedResult = results.FirstOrDefault();

        await InvokeAsync(StateHasChanged);
    }
}