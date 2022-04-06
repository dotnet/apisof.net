using Mono.Options;

internal abstract class Command
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual void AddOptions(OptionSet options)
    {
    }

    public abstract void Execute();
}
