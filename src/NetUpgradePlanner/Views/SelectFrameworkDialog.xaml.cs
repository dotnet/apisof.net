using System.Windows;

using NetUpgradePlanner.Mvvm;

using NuGet.Frameworks;

using Terrajobst.ApiCatalog;

namespace NetUpgradePlanner.Views;

internal sealed partial class SelectFrameworkDialog : Window
{
    private readonly SelectFrameworkDialogViewModel _viewModel;

    public SelectFrameworkDialog(ApiCatalogModel catalog, string framework)
    {
        InitializeComponent();

        _viewModel = new SelectFrameworkDialogViewModel(catalog, framework);

        DataContext = _viewModel;
    }

    public string Framework => _viewModel.Framework.Name;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private sealed class SelectFrameworkDialogViewModel : ViewModel
    {
        private FrameworkViewModel _framework;

        public SelectFrameworkDialogViewModel(ApiCatalogModel catalog, string framework)
        {
            Frameworks = GetFrameworks(catalog);
            _framework = Frameworks.Single(f => string.Equals(f.Name, framework, StringComparison.OrdinalIgnoreCase));
        }

        public FrameworkViewModel Framework
        {
            get => _framework;
            set
            {
                if (_framework != value)
                {
                    _framework = value;
                    OnPropertyChanged();
                }
            }
        }

        public IReadOnlyList<FrameworkViewModel> Frameworks { get; }

        private static IReadOnlyList<FrameworkViewModel> GetFrameworks(ApiCatalogModel catalog)
        {
            return catalog.Frameworks
                          .Select(f => new FrameworkViewModel(f.Name))
                          .Where(f => f.Framework.Framework is ".NETFramework" or ".NETCoreApp" or ".NETStandard")
                          .OrderBy(n => n.Framework.Framework)
                          .ThenBy(n => n.Framework.Version)
                          .ThenBy(n => n.Framework.Platform)
                          .ToArray();

        }
    }

    private sealed class FrameworkViewModel : ViewModel
    {
        public FrameworkViewModel(string name)
        {
            var framework = NuGetFramework.Parse(name);
            var platformSuffix = framework.HasPlatform
                                  ? $" ({PlatformAnnotationEntry.FormatPlatform(framework.Platform)})"
                                  : string.Empty;
            var displayName = $"{framework.GetFrameworkDisplayString()} {framework.Version.GetVersionDisplayString()}{platformSuffix}";

            Framework = framework;
            DisplayName = displayName;
            Name = name;
        }

        public NuGetFramework Framework { get; }

        public string DisplayName { get; }

        public string Name { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
