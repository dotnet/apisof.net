using System.Net;
using System.Text;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public sealed class DocumentationResolverService
{
    private static readonly string[] _targets = [
        "https://learn.microsoft.com/dotnet/api/",
        "https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/",
        "https://learn.microsoft.com/uwp/api/"
    ];

    private readonly HttpClient _client;

    public DocumentationResolverService(HttpClient client)
    {
        ThrowIfNull(client);

        _client = client;
    }

    public async Task<string?> ResolveAsync(ApiModel api)
    {
        ThrowIfDefault(api);

        var helpLinks = GetHelpLinks(api).ToArray();
        var tasks = helpLinks.Select(_client.GetAsync).ToArray();
        await Task.WhenAll(tasks);
        try
        {
            for (var i = 0; i < tasks.Length; i++)
            {
                var response = tasks[i].Result;
                if (response.StatusCode == HttpStatusCode.OK)
                    return helpLinks[i];
            }
        }
        finally
        {
            foreach (var task in tasks)
                task.Result.Dispose();
        }

        return null;
    }

    private static IEnumerable<string> GetHelpLinks(ApiModel api)
    {
        var path = GetHelpPath(api);
        return _targets.Select(t => t + path);
    }

    private static string GetHelpPath(ApiModel api)
    {
        var segments = api.AncestorsAndSelf().Reverse();

        var sb = new StringBuilder();
        var inAngleBrackets = false;
        var numberOfGenerics = 0;

        foreach (var s in segments)
        {
            if (sb.Length > 0)
                sb.Append('.');

            foreach (var c in s.Name)
            {
                if (inAngleBrackets)
                {
                    if (c == ',')
                    {
                        numberOfGenerics++;
                    }
                    else if (c == '>')
                    {
                        inAngleBrackets = false;

                        if (s.Kind.IsType())
                        {
                            sb.Append('-');
                            sb.Append(numberOfGenerics);
                        }
                    }
                    continue;
                }

                if (c == '(')
                {
                    break;
                }
                else if (c == '<')
                {
                    inAngleBrackets = true;
                    numberOfGenerics = 1;
                    continue;
                }
                else
                {
                    sb.Append(char.ToLower(c));
                }
            }
        }

        var path = sb.ToString();

        return path;
    }
}
