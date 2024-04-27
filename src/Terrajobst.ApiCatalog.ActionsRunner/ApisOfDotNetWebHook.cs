namespace Terrajobst.ApiCatalog.ActionsRunner;

public abstract class ApisOfDotNetWebHook
{
    public abstract Task InvokeAsync(ApisOfDotNetWebHookSubject subject);
}