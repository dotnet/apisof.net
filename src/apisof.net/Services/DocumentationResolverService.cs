using System.Net;
using System.Text;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public sealed class DocumentationResolverService
{
    private readonly HttpClient _client;

    public DocumentationResolverService(HttpClient client)
    {
        ThrowIfNull(client);
        
        _client = client;
    }

    public async Task<string?> ResolveAsync(ApiModel api)
    {
        ThrowIfDefault(api);

        var helpLink = GetHelpLink(api);
        using var response = await _client.GetAsync(helpLink);
        return response.StatusCode == HttpStatusCode.OK ? helpLink : null;
    }
    
    private static string GetHelpLink(ApiModel api)
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

        return $"https://docs.microsoft.com/en-us/dotnet/api/{path}";
    }
}