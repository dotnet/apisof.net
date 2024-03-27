#nullable enable

using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Services;

public sealed class SourceResolverService
{
    private static readonly IReadOnlyList<string> KnownSourceServers = [
        "https://source.dot.net",
        "https://sourceroslyn.io"
    ];

    private readonly HttpClient _client;

    public SourceResolverService(HttpClient client)
    {
        _client = client;
    }

    public async Task<string?> ResolveAsync(ApiModel api)
    {
        var serverId = GetSourceServerId(api.Guid);

        foreach (var server in KnownSourceServers)
        {
            var url = $"{server}/api/symbolurl?symbolId={serverId}";

            try
            {
                var result = await _client.GetStringAsync(url);
                return $"{server}{result}";
            }
            catch
            {
                // Ignore
            }
        }

        return null;
    }

    private static string GetSourceServerId(Guid id)
    {
        var bytes = (Span<byte>)stackalloc byte[16];
        id.TryWriteBytes(bytes);

        var ulongBytes = bytes.Slice(0, 8);
        return Convert.ToHexString(ulongBytes);
    }
}