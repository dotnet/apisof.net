using System;
using System.Windows;
using System.Windows.Navigation;

using NetUpgradePlanner.Services;

namespace NetUpgradePlanner.Views;

internal sealed partial class TelemetryDialog : Window
{
    private readonly TelemetryService _telemetryService;

    public TelemetryDialog(TelemetryService telemetryService)
    {
        InitializeComponent();

        _telemetryService = telemetryService;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _telemetryService.IsEnabled = CheckBox.IsChecked is true;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = BrowserService.NavigateTo(e.Uri);
    }
}
