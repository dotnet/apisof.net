
using ApiCatalog.Services;

using Microsoft.AspNetCore.Components;

namespace ApiCatalog.Shared
{
    public partial class SetQueryParameter
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public string Key { get; set; }

        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        public string Uri { get; set; }

        protected override void OnParametersSet()
        {
            Uri = NavigationManager.SetQueryParameter(Key, Value);
        }
    }
}