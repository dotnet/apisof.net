using Mono.Options;

namespace Terrajobst.ApiCatalog.ActionsRunner;

internal sealed class ConsoleMainForCommands : IConsoleMain
{
    private readonly IEnumerable<ConsoleCommand> _commands;

    public ConsoleMainForCommands(IEnumerable<ConsoleCommand> commands)
    {
        ThrowIfNull(commands);

        _commands = commands;
    }

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var commandName = args.FirstOrDefault();

        if (commandName is null ||
            commandName is "-?" or "-h" or "--help")
        {
            var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            Console.Error.WriteLine($"usage: {appName} <command> [OPTIONS]+");
            Console.Error.WriteLine();

            foreach (var c in _commands)
                Console.Error.WriteLine($"  {c.Name,-25}{c.Description}");

            return;
        }

        var command = _commands.SingleOrDefault(c => c.Name == commandName);
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
}