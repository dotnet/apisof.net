using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ApiCatalogWeb.Shared
{
    public partial class ApiSearch
    {
        private ElementReference _inputElement;
        private string _modalDisplay = "none;";
        private string _modalClass = "";
        private CancellationTokenSource _cts;

        private string SearchText { get; set; }
        private CatalogSearchResult[] SearchResults { get; set; }
        private CatalogSearchResult SelectedResult { get; set; }

        [Inject]
        private CatalogService CatalogService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        public bool IsOpen { get; private set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsOpen)
                await _inputElement.FocusAsync();

            if (firstRender)
            {
                var helper = new NextAndPreviousHelper(SelectedPrevious, SelectedNext, Accept);
                var helperReference = DotNetObjectReference.Create(helper);
                await JSRuntime.InvokeVoidAsync("registerSearchInputKeyDown", _inputElement, helperReference);
            }
        }

        private void SelectedPrevious()
        {
            if (SelectedResult == null || SearchResults.Length == 0)
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
            if (SelectedResult == null || SearchResults.Length == 0)
                SelectedResult = SearchResults.FirstOrDefault();
            else
            {
                var index = Array.IndexOf(SearchResults, SelectedResult);
                if (index < SearchResults.Length - 1)
                    index++;

                SelectedResult = SearchResults[index];
            }
        }

        private void Accept()
        {
            if (SelectedResult != null)
            {
                NavigationManager.NavigateTo($"/catalog/{SelectedResult.ApiGuid}");
                Close();
            }
        }

        public async Task Open()
        {
            _modalDisplay = "block;";
            _modalClass = "Show";
            SearchText = "";
            SearchResults = Array.Empty<CatalogSearchResult>();
            SelectedResult = null;
            IsOpen = true;
        }

        public async Task Close()
        {
            _modalDisplay = "none";
            _modalClass = "";
            IsOpen = false;
            await OnClose.InvokeAsync();
        }

        private async Task UpdateSearchResults(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();

            if (_cts != null)
                _cts.Cancel();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var results = (await CatalogService.Search(SearchText)).ToArray();

            if (!token.IsCancellationRequested)
            {
                SearchResults = results;
                SelectedResult = results.FirstOrDefault();
            }
        }

        public class NextAndPreviousHelper
        {
            private readonly Action _previous;
            private readonly Action _next;
            private readonly Action _accept;

            public NextAndPreviousHelper(Action previous, Action next, Action accept)
            {
                _previous = previous;
                _next = next;
                _accept = accept;
            }

            [JSInvokable]
            public void SelectPrevious()
            {
                _previous.Invoke();
            }

            [JSInvokable]
            public void SelectNext()
            {
                _next.Invoke();
            }

            [JSInvokable]
            public void Accept()
            {
                _accept.Invoke();
            }
        }
    }
}