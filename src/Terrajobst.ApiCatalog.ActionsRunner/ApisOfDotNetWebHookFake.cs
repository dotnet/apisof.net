namespace Terrajobst.ApiCatalog.ActionsRunner;

internal sealed class ApisOfDotNetWebHookFake : ApisOfDotNetWebHook
{
    public override Task InvokeAsync()
    {
        Console.WriteLine("Invoking web hook suppressed for development.");
        return Task.CompletedTask;
    }
}