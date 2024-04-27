using Microsoft.Extensions.Hosting;
using Terrajobst.ApiCatalog.ActionsRunner;

var builder = ConsoleHost.CreateApplicationBuilder();

builder.AddApisOfDotNetPathProvider();
builder.AddApisOfDotNetStore();
builder.AddScratchFileProvider();
builder.AddMainWithCommands();

var app = builder.Build();
await app.RunAsync();
