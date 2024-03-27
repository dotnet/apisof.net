using ApisOfDotNet.Services;

namespace ApisOfDotNet;

// TODO: Use new style and merge Program.cs and Startup.cs

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddControllers();
        services.AddSingleton<CatalogService>();
        services.AddSingleton<IconService>();
        services.AddSingleton<DocumentationResolverService>();
        services.AddHttpClient<DocumentationResolverService>();
        services.AddSingleton<SourceResolverService>();
        services.AddHttpClient<SourceResolverService>();
        services.AddHostedService<CatalogServiceWarmUp>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
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

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapDefaultControllerRoute();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}