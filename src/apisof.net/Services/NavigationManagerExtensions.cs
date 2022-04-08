using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace ApisOfDotNet.Services;

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

        var newParameters = parameters.SelectMany(kvp => kvp.Value, (kvp, v) => KeyValuePair.Create<string, string>(kvp.Key, v))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return QueryHelpers.AddQueryString(uri.ToString(), newParameters);
    }
}