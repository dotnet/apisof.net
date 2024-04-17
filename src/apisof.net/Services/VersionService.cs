using System.Net;
using System.Reflection;

namespace ApisOfDotNet.Services;

public sealed class VersionService
{
    private readonly HttpClient _client;
    private CommitInfo? _versionResult;

    public VersionService(HttpClient client)
    {
        ThrowIfNull(client);

        _client = client;
    }

    public async ValueTask<(string Title, string Url, string Hash)> GetCommitAsync()
    {
        if (_versionResult is null)
        {
            var versionResult = await GetCommitNoCacheAsync();
            Interlocked.CompareExchange(ref _versionResult, versionResult, null);
        }

        return (_versionResult.Title, _versionResult.Url, _versionResult.Hash);
    }

    private async Task<CommitInfo> GetCommitNoCacheAsync()
    {
        var informationalVersion = GetType().Assembly
            .GetCustomAttributesData()
            .Where(ca => ca.AttributeType == typeof(AssemblyInformationalVersionAttribute))
            .SelectMany(ca => ca.ConstructorArguments.Select(a => a.Value as string))
            .FirstOrDefault(string.Empty)!;

        var indexOfPlus = informationalVersion.IndexOf('+');
        var hash = indexOfPlus >= 0
            ? informationalVersion.Substring(indexOfPlus + 1)
            : null;

        if (hash is null)
            return new CommitInfo("<Unknown>", "about:blank", string.Empty);

        var url = $"https://github.com/dotnet/apisof.net/commit/{hash}";
        var title = hash;

        var patchUrl = url + ".patch";
        using var response = await _client.GetAsync(patchUrl);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var remainingLines = 5;

            const string prefix = "Subject: [Patch]";
            while (remainingLines-- > 0 && await reader.ReadLineAsync() is { } line)
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    title = line.Substring(prefix.Length).Trim();
                    break;
                }
            }
        }

        return new CommitInfo(title, url, hash);
    }


    private record CommitInfo(string Title, string Url, string Hash);
}