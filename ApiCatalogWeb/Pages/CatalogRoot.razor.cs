using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApiCatalogWeb.Services;
using Microsoft.AspNetCore.Components;

namespace ApiCatalogWeb.Pages
{
    public partial class CatalogRoot
    {
        [Inject]
        public CatalogService CatalogService { get; set; }

        public IReadOnlyList<CatalogApi> Namespaces { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Namespaces = await CatalogService.GetNamespacesAsync();
        }
    }
}
