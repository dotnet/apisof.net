using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Terrajobst.ApiCatalog.ActionsRunner;

namespace Terrajobst.ApiCatalog.Tests;

public class ApisOfDotNetWebHookTests
{
    [Theory]
    [InlineData(ApisOfDotNetWebHookSubject.ApiCatalog, "job.json")]
    [InlineData(ApisOfDotNetWebHookSubject.DesignNotes, "designNotes.dat")]
    [InlineData(ApisOfDotNetWebHookSubject.UsageData, "usageData.dat")]
    public void OmittedUrl_PassesValidationAndUsesProduction(ApisOfDotNetWebHookSubject subject, string blobName)
    {
        using var services = CreateServices(null);
        var options = services.GetRequiredService<IOptions<ApisOfDotNetWebHookOptions>>();
        var webHook = new ApisOfDotNetWebHookReal(options);

        Assert.Equal($"https://apisof.net/webhook?subject={blobName}", webHook.GetWebHookUrl(subject));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a URL")]
    public void InvalidUrl_FailsValidation(string url)
    {
        using var services = CreateServices(url);

        Assert.Throws<OptionsValidationException>(() =>
            services.GetRequiredService<IOptions<ApisOfDotNetWebHookOptions>>().Value);
    }

    [Theory]
    [InlineData("https://example.com/webhook")]
    [InlineData("https://example.com/webhook?subject=")]
    [InlineData("https://example.com/webhook?subject=old&subject=older")]
    public void ConfiguredUrl_SetsExactlyOneSubject(string url)
    {
        using var services = CreateServices(url);
        var webHook = new ApisOfDotNetWebHookReal(services.GetRequiredService<IOptions<ApisOfDotNetWebHookOptions>>());

        Assert.Equal("https://example.com/webhook?subject=job.json",
                     webHook.GetWebHookUrl(ApisOfDotNetWebHookSubject.ApiCatalog));
    }

    [Fact]
    public void ConfiguredUrl_PreservesOtherQueryParametersAndFragment()
    {
        using var services = CreateServices("https://example.com/webhook?tag=a%26b&subject=old&tag=c&empty=#section");
        var webHook = new ApisOfDotNetWebHookReal(services.GetRequiredService<IOptions<ApisOfDotNetWebHookOptions>>());

        var uri = new Uri(webHook.GetWebHookUrl(ApisOfDotNetWebHookSubject.DesignNotes));
        var query = QueryHelpers.ParseQuery(uri.Query);

        Assert.Equal("/webhook", uri.AbsolutePath);
        Assert.Equal("#section", uri.Fragment);
        Assert.Equal(3, query.Count);
        Assert.Equal(new[] { "a&b", "c" }, query["tag"].ToArray());
        Assert.Equal(string.Empty, query["empty"].ToString());
        Assert.Equal("designNotes.dat", Assert.Single(query["subject"]));
    }

    private static ServiceProvider CreateServices(string? url)
    {
        var values = new Dictionary<string, string?>
        {
            ["ApisOfDotNetWebHookSecret"] = "test-secret"
        };
        if (url is not null)
            values["ApisOfDotNetWebHookUrl"] = url;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ApisOfDotNetWebHookOptions>()
                .BindConfiguration("")
                .ValidateDataAnnotations()
                .ValidateOnStart();
        return services.BuildServiceProvider();
    }
}
