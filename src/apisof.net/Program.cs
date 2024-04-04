using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ApisOfDotNetOptions>()
                .BindConfiguration("")
                .ValidateDataAnnotations()
                .ValidateOnStart();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddHotKeys2(); 
builder.Services.AddSingleton<CatalogService>();
builder.Services.AddSingleton<DocumentationResolverService>();
builder.Services.AddHttpClient<DocumentationResolverService>();
builder.Services.AddSingleton<SourceResolverService>();
builder.Services.AddHttpClient<SourceResolverService>();
builder.Services.AddHostedService<CatalogServiceWarmUp>();
builder.Services.AddScoped<QueryManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapDefaultControllerRoute();
app.MapFallbackToPage("/_Host");

app.Run();