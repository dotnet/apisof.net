using System.Collections.Generic;
using System.Threading.Tasks;

using ApiCatalogWeb.Services;

using Microsoft.AspNetCore.Components;

namespace ApiCatalogWeb.Pages
{
    public partial class CatalogRoot
    {
        [Inject]
        public CatalogService CatalogService { get; set; }
    }
}
