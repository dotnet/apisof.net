namespace Terrajobst.NetUpgradePlanner;

public interface IProgressMonitor
{
    static IProgressMonitor Empty { get; } = EmptyProgressMonitor.Instance;

    void Report(int value, int maximum);

    private sealed class EmptyProgressMonitor : IProgressMonitor
    {
        public static EmptyProgressMonitor Instance { get; } = new EmptyProgressMonitor();

        private EmptyProgressMonitor()
        {
        }

        public void Report(int value, int maximum)
        {
        }
    }
}
