using System.Diagnostics;

using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Services;

internal sealed class ProgressService
{
    private readonly ProgressMonitor _progressMonitor;

    public ProgressService()
    {
        _progressMonitor = new ProgressMonitor(this);
    }

    public bool IsRunning { get; private set; }

    public float? Percentage { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public Task Run(Action<IProgressMonitor> operation, string text)
    {
        return Run(pm =>
        {
            operation(pm);
            return Task.CompletedTask;
        }, text);
    }

    public Task<T> Run<T>(Func<IProgressMonitor, T> operation, string text)
    {
        return Run(pm =>
        {
            var result = operation(pm);
            return Task.FromResult(result);
        }, text);
    }

    public async Task Run(Func<IProgressMonitor, Task> operation, string text)
    {
        Start(text);
        try
        {
            await Task.Run(() => operation(_progressMonitor));
        }
        finally
        {
            Stop();
        }
    }

    public async Task<T> Run<T>(Func<IProgressMonitor, Task<T>> operation, string text)
    {
        T result = default!;

        await Run(async pm =>
        {
            result = await operation(pm);
        }, text);

        return result;
    }

    private void Start(string text)
    {
        if (IsRunning)
            throw new InvalidOperationException();

        Percentage = null;
        IsRunning = true;
        Text = text;
        OnChanged();
    }

    private void Stop()
    {
        IsRunning = false;
        Text = string.Empty;
        OnChanged();
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Report(int value, int maximum)
    {
        Percentage = Math.Clamp((float)value / maximum, 0.0f, 1.0f);
        OnChanged();
    }

    public event EventHandler? Changed;

    internal sealed class ProgressMonitor : IProgressMonitor
    {
        private readonly ProgressService _owner;
        private readonly SynchronizationContext _context;

        public ProgressMonitor(ProgressService owner)
        {
            Debug.Assert(SynchronizationContext.Current is not null);

            _context = SynchronizationContext.Current;
            _owner = owner;
        }

        public void Report(int value, int maximum)
        {
            _context.Post(_ => _owner.Report(value, maximum), null);
        }
    }
}
