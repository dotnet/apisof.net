using System;

namespace ApiCatalog.Services
{
    public class CatalogJobInfo
    {
        public DateTimeOffset Date { get; set; }
        public bool Success { get; set; }
        public string DetailsUrl { get; set; }
    }
}
