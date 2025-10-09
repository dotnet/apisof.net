using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ApisOfDotNet.Shared;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<AzureBlobClientManager>();
    })
    .Build();
host.Run();
