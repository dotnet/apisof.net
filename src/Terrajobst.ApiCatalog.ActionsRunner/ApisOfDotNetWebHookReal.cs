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

    public override async Task InvokeAsync()
    {
        Console.WriteLine("Invoking web hook...");
        var url = _options.Value.GenCatalogWebHookUrl;
        var secret = _options.Value.GenCatalogWebHookSecret;

        var client = new HttpClient();
        var response = await client.PostAsync(url, new StringContent(secret));
        Console.WriteLine($"Webhook returned: {response.StatusCode}");
    }
}