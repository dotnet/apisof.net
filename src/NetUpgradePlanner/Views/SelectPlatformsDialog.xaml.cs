using System.Windows;

using NetUpgradePlanner.Mvvm;

using Terrajobst.ApiCatalog;
using Terrajobst.NetUpgradePlanner;

namespace NetUpgradePlanner.Views;

internal sealed partial class SelectPlatformsDialog : Window
{
    private readonly SelectPlatformsDialogViewModel _viewModel;

    public SelectPlatformsDialog(ApiCatalogModel catalog, PlatformSet platforms)
    {
        InitializeComponent();

        _viewModel = new SelectPlatformsDialogViewModel(catalog, platforms);

        DataContext = _viewModel;
    }

    public PlatformSet Platforms => _viewModel.GetPlatformSet();

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private sealed class SelectPlatformsDialogViewModel : ViewModel
    {
        private bool _any;
        private bool _specific;

        public SelectPlatformsDialogViewModel(ApiCatalogModel catalog, PlatformSet platforms)
        {
            var knownPlatforms = GetKnownPlatforms(catalog);

            Any = platforms.IsAny;
            Specific = platforms.IsSpecific;
            Platforms = knownPlatforms.Select(n => new PlatformViewModel(n, isChecked: platforms.Platforms.Contains(n)))
                                      .ToArray();
        }

        private static IReadOnlyList<string> GetKnownPlatforms(ApiCatalogModel catalog)
        {
            return catalog.Platforms
                          .Select(p => PlatformAnnotationContext.ParsePlatform(p.Name).Name.ToLower())
                          .Distinct()
                          .OrderBy(n => n)
                          .ToArray();
        }

        public bool Any
        {
            get => _any;
            set
            {
                if (_any != value)
                {
                    _any = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Specific
        {
            get => _specific;
            set
            {
                if (_specific != value)
                {
                    _specific = value;
                    OnPropertyChanged();
                }
            }
        }

        public IReadOnlyList<PlatformViewModel> Platforms { get; }

        public PlatformSet GetPlatformSet()
        {
            if (Any)
                return PlatformSet.Any;
            else
                return PlatformSet.For(Platforms.Where(vm => vm.IsChecked).Select(vm => vm.Name));
        }
    }

    internal sealed class PlatformViewModel : ViewModel
    {
        private bool _isChecked;

        public PlatformViewModel(string name, bool isChecked)
        {
            Name = name;
            DisplayName = PlatformAnnotationEntry.FormatPlatform(Name);
            IsChecked = isChecked;
        }

        public string Name { get; }

        public string DisplayName { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
