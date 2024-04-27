namespace Terrajobst.ApiCatalog.ActionsRunner;

public interface IConsoleMain
{
    Task RunAsync(string[] args, CancellationToken cancellationToken);
}