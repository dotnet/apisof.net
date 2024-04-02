using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Terrajobst.ApiCatalog;

using Timer = System.Timers.Timer;

namespace ApisOfDotNet.Shared;

public partial class ApiSearch
{
    private ElementReference _inputElement;
    private string _modalDisplay = "none;";
    private string _modalClass = "";
    private Timer _debounceTimer;
    private string _searchText;

    private string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            RestartDebounce();
        }
    }

    private ApiModel[] SearchResults { get; set; }
    private ApiModel SelectedResult { get; set; }

    [Inject]
    private CatalogService CatalogService { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    public bool IsOpen { get; private set; }

    protected override void OnInitialized()
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen)
            await _inputElement.FocusAsync();

        if (firstRender)
        {
            var searchText = NavigationManager.GetQueryParameter("q");
            if (!string.IsNullOrEmpty(searchText))
            {
                Open();
                SearchText = searchText;
            }

            var helper = new NextAndPreviousHelper(Open, SelectedPrevious, SelectedNext, Accept);
            var helperReference = DotNetObjectReference.Create(helper);
            await JSRuntime.InvokeVoidAsync("registerSearchInputKeyDown", _inputElement, helperReference);
        }
    }

    private async void OnDebounceTimerElapsed(object sender, EventArgs args)
    {
        _debounceTimer.Stop();

        await UpdateSearchResults();
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
        _modalDisplay = "block;";
        _modalClass = "Show";
        SearchText = "";
        SearchResults = Array.Empty<ApiModel>();
        SelectedResult = default;
        IsOpen = true;
        StateHasChanged();
    }

    public async void Close()
    {
        _modalDisplay = "none";
        _modalClass = "";
        IsOpen = false;
        await OnClose.InvokeAsync();
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

    public class NextAndPreviousHelper
    {
        private readonly Action _showSearch;
        private readonly Action _previous;
        private readonly Action _next;
        private readonly Action _accept;

        public NextAndPreviousHelper(Action showSearch, Action previous, Action next, Action accept)
        {
            _showSearch = showSearch;
            _previous = previous;
            _next = next;
            _accept = accept;
        }

        [JSInvokable]
        public void ShowSearch() => _showSearch.Invoke();

        [JSInvokable]
        public void SelectPrevious() => _previous.Invoke();

        [JSInvokable]
        public void SelectNext() => _next.Invoke();

        [JSInvokable]
        public void Accept() => _accept.Invoke();
    }
}