using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using NetUpgradePlanner.Services;
using NetUpgradePlanner.ViewModels.MainWindow;

namespace NetUpgradePlanner.Views;

internal sealed partial class ProblemListView : UserControl
{
    public ProblemListView(ProblemListViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var expression = FilterTextBox.GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource();
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = BrowserService.NavigateTo(e.Uri);
    }
}
