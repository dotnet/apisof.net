using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetUpgradePlanner.Mvvm;

internal abstract class ViewModel : INotifyPropertyChanged
{
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var e = new PropertyChangedEventArgs(propertyName);
        OnPropertyChanged(e);
    }

    protected void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
