namespace Terrajobst.ApiCatalog.ActionsRunner;

internal sealed class ApisOfDotNetWebHookFake : ApisOfDotNetWebHook
{
    public override Task InvokeAsync(ApisOfDotNetWebHookSubject subject)
    {
        Console.WriteLine($"Invoking web hook for {subject} suppressed for development.");
        return Task.CompletedTask;
    }
}