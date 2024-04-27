using Microsoft.Extensions.Options;

namespace Terrajobst.ApiCatalog.ActionsRunner;

internal sealed class ApisOfDotNetWebHookReal : ApisOfDotNetWebHook
{
    private readonly IOptions<ApisOfDotNetWebHookOptions> _options;

    public ApisOfDotNetWebHookReal(IOptions<ApisOfDotNetWebHookOptions> options)
    {
        ThrowIfNull(options);

        _options = options;
    }

    public override async Task InvokeAsync(ApisOfDotNetWebHookSubject subject)
    {
        Console.WriteLine($"Invoking web hook for {subject}...");
        var url = GetWebHookUrl(subject);
        var secret = _options.Value.ApisOfDotNetWebHookSecret;

        var client = new HttpClient();
        var response = await client.PostAsync(url, new StringContent(secret));
        Console.WriteLine($"Webhook returned: {response.StatusCode}");
    }

    private static string GetWebHookUrl(ApisOfDotNetWebHookSubject subject)
    {
        var blobName = GetBlobName(subject);
        return $"https://apisof.net/webhook?subject={blobName}";
    }

    private static string GetBlobName(ApisOfDotNetWebHookSubject subject)
    {
        switch (subject)
        {
            case ApisOfDotNetWebHookSubject.ApiCatalog:
                return "job.json";
            case ApisOfDotNetWebHookSubject.DesignNotes:
                return "designNotes.dat";
            case ApisOfDotNetWebHookSubject.UsageData:
                return "usageData.dat";
            default:
                throw new ArgumentOutOfRangeException(nameof(subject), subject, null);
        }
    }
}