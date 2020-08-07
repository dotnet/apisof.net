using System;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

using NuGet.Packaging.Signing;

namespace ApiCatalogWeb.Services
{
    public static class NavigationManagerExtensions
    {
        public static string GetQueryParameter(this NavigationManager navigationManager, string key)
        {
            var uri = new Uri(navigationManager.Uri);
            var parameters = QueryHelpers.ParseQuery(uri.Query);
            if (parameters.TryGetValue(key, out var values))
                return values.ToString();

            return null;
        }

        public static string SetQueryParameter(this NavigationManager navigationManager, string key, string value)
        {
            var uri = new UriBuilder(navigationManager.Uri);
            var parameters = QueryHelpers.ParseQuery(uri.Query);
            uri.Query = null;
            parameters.Remove(key);
            parameters.Add(key, value);

            return QueryHelpers.AddQueryString(uri.ToString(), parameters);
        }
    }
}
