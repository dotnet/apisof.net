using GenCatalog;
using Microsoft.Extensions.Hosting;

using Terrajobst.ApiCatalog.ActionsRunner;

if (args.Length > 1)
{
    var exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
    Console.Error.Write("error: incorrect number of arguments");
    Console.Error.Write($"usage: {exeName} [<download-directory>]");
    return -1;
}

try
{
    var builder = ConsoleHost.CreateApplicationBuilder();

    builder.AddApisOfDotNetPathProvider();
    builder.AddApisOfDotNetStore();
    builder.AddApisOfDotNetWebHook();
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