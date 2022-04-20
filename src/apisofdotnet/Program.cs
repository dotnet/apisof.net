using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mono.Options;

using Spectre.Console;

internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            await RunAsync(args);
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Console.Error.WriteLine("fatal:");
            AnsiConsole.WriteException(ex);
        }
    }

    private static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
                          .ConfigureServices((_, services) => ConfigureServices(services))
                          .Build();

        var commands = builder.Services.GetRequiredService<IEnumerable<Command>>();

        var commandName = args.FirstOrDefault();

        if (commandName is null ||
            commandName is "-?" or "-h" or "--help")
        {
            var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            Console.Error.WriteLine($"usage: {appName} <command> [OPTIONS]+");
            Console.Error.WriteLine();

            foreach (var c in commands)
                Console.Error.WriteLine($"  {c.Name,-25}{c.Description}");

            return;
        }

        var command = commands.SingleOrDefault(c => c.Name == commandName);
        if (command is null)
        {
            Console.Error.WriteLine($"error: undefined command '{commandName}'");
            return;
        }

        var help = false;

        var options = new OptionSet();
        command.AddOptions(options);
        options.Add("?|h|help", null, _ => help = true, true);

        try
        {
            var unprocessed = options.Parse(args.Skip(1));

            if (help)
            {
                var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
                Console.Error.WriteLine(command.Description);
                Console.Error.WriteLine($"usage: {appName} {command.Name} [OPTIONS]+");
                options.WriteOptionDescriptions(Console.Error);
                return;
            }

            if (unprocessed.Any())
            {
                foreach (var option in unprocessed)
                    Console.Error.WriteLine($"error: unrecognized argument {option}");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return;
        }

        await command.ExecuteAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddCommands<Program>();
        services.AddSingleton<CatalogService>();
    }
}