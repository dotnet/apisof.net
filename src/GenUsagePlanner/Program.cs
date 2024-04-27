using GenUsagePlanner;
using Microsoft.Extensions.Hosting;
using Terrajobst.ApiCatalog.ActionsRunner;

try
{
    var builder = ConsoleHost.CreateApplicationBuilder();

    builder.AddApisOfDotNetPathProvider();
    builder.AddApisOfDotNetStore();
    builder.AddScratchFileProvider();
    builder.AddMain<Main>();

    var app = builder.Build();
    await app.RunAsync();
    return 0;
}
catch (Exception e)
{
    Console.WriteLine(e);
    return -1;
}