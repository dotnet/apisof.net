using Mono.Options;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public abstract class ConsoleCommand
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual void AddOptions(OptionSet options)
    {
    }

    public abstract Task ExecuteAsync();
}