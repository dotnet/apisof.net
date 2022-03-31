
using ApiCatalog;

namespace ApiCatalogWeb.Services
{
    public class IconService
    {
        public string GetIcon(ApiKind kind)
        {
            var name = kind.ToString();

            if (kind is ApiKind.Constructor or
                        ApiKind.Destructor or
                        ApiKind.PropertyGetter or
                        ApiKind.PropertySetter or
                        ApiKind.EventAdder or
                        ApiKind.EventRemover or
                        ApiKind.EventRaiser)
            {
                name = "method";
            }

            return $"/img/{name}.svg";
        }
    }
}
