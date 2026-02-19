using GenDesignNotes;
using Microsoft.Extensions.Hosting;
using Terrajobst.ApiCatalog.ActionsRunner;

var builder = ConsoleHost.CreateApplicationBuilder();

builder.AddApisOfDotNetPathProvider();
builder.AddApisOfDotNetStore();
builder.AddApisOfDotNetWebHook();
builder.AddMain<Main>();

var app = builder.Build();
await app.RunAsync();
