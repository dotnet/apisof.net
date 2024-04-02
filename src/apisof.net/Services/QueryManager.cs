#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace ApisOfDotNet.Services;

public sealed class QueryManager : IDisposable
{
    private readonly NavigationManager _navigationManager;
    private string _previousLocation;
    
    public QueryManager(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += NavigationManagerOnLocationChanged;
        _previousLocation = navigationManager.Uri;
    }

    public string GetQueryParameter(string name)
    {
        return _navigationManager.GetQueryParameter(name);
    }
    
    public void Dispose()
    {
        _navigationManager.LocationChanged -= NavigationManagerOnLocationChanged;
    }

    private void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateUri(e.Location);
    }

    private void UpdateUri(string newLocation)
    {
        var previousWithoutQuery = new UriBuilder(new Uri(_previousLocation)) {
            Query = null
        }.ToString();
        
        var newWithoutQuery = new UriBuilder(new Uri(newLocation)) {
            Query = null
        }.ToString();

        if (previousWithoutQuery == newWithoutQuery)
            DiffQueryParameters(newLocation);

        _previousLocation = newLocation;
    }

    private void DiffQueryParameters(string newLocation)
    {
        var previousQuery = QueryHelpers.ParseQuery(new Uri(_previousLocation).Query);
        var newQuery = QueryHelpers.ParseQuery(new Uri(newLocation).Query);

        var changedParameters = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, previousValue) in previousQuery)
        {
            if (!newQuery.TryGetValue(key, out var newValue) || previousValue != newValue)
                changedParameters.Add(key);
        }
        
        foreach (var key in newQuery.Keys)
        {
            if (!previousQuery.ContainsKey(key))
                changedParameters.Add(key);
        }
        
        if (changedParameters.Any())
            QueryChanged?.Invoke(this, changedParameters);
    }

    public event EventHandler<IReadOnlySet<string>>? QueryChanged;
}