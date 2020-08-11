
using ApiCatalog;

namespace ApiCatalogWeb.Services
{
    public class IconService
    {
        public string GetIcon(ApiKind kind)
        {
            var name = kind.ToString();

            if (kind == ApiKind.Constructor ||
                kind == ApiKind.Destructor)
            {
                name = "method";
            }

           return $"/img/{name}.svg";
        }
    }
}
