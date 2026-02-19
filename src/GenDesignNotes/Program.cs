using GenDesignNotes;
using Microsoft.Extensions.Hosting;
using Terrajobst.ApiCatalog.ActionsRunner;
using Azure.Core.Diagnostics;
using System.Diagnostics.Tracing;
var builder = ConsoleHost.CreateApplicationBuilder();

AzureEventSourceListener.CreateConsoleLogger(EventLevel.Verbose);
builder.AddApisOfDotNetPathProvider();
builder.AddApisOfDotNetStore();
builder.AddApisOfDotNetWebHook();
builder.AddMain<Main>();

var app = builder.Build();
await app.RunAsync();
