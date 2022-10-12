using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

using NetUpgradePlanner.Services;
using NetUpgradePlanner.ViewModels.MainWindow;

namespace NetUpgradePlanner.Views;

internal sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TelemetryService _telemetryService;

    public MainWindow(MainWindowViewModel viewModel,
                      AssemblyListView assemblyListView,
                      ProblemListView problemListView,
                      GraphView graphView,
                      TelemetryService telemetryService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        AssemblyListViewHost.Content = assemblyListView;
        ProblemListViewHost.Content = problemListView;
        GraphViewHost.Content = graphView;
        _telemetryService = telemetryService;

        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() => OnShown(), DispatcherPriority.Input);
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        var result = _viewModel.ConfirmSave();

        if (result is null)
        {
            e.Cancel = true;
        }
        else if (result is true)
        {
            await _viewModel.SaveAsync();
        }

        base.OnClosing(e);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnShown()
    {
#if !DEBUG
        if (!_telemetryService.IsConfigured())
        {
            var dialog = new TelemetryDialog(_telemetryService);
            dialog.Owner = this;
            dialog.ShowDialog();
        }
#endif
    }
}
