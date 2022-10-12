using System;

using NetUpgradePlanner.Mvvm;
using NetUpgradePlanner.Services;

namespace NetUpgradePlanner.ViewModels.MainWindow;

internal sealed class ProgressViewModel : ViewModel
{
    private readonly ProgressService _progressService;
    private bool _isRunning;
    private bool _hasPercentage;
    private string _text = string.Empty;
    private float _percentage = 0.0f;

    public ProgressViewModel(ProgressService progressService)
    {
        _progressService = progressService;
        _progressService.Changed += ProgressService_Changed;
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasPercentage
    {
        get => _hasPercentage;
        private set
        {
            if (_hasPercentage != value)
            {
                _hasPercentage = value;
                OnPropertyChanged();
            }
        }
    }

    public float Percentage
    {
        get => _percentage;
        private set
        {
            if (_percentage != value)
            {
                _percentage = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text
    {
        get => _text;
        private set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    private void ProgressService_Changed(object? sender, EventArgs e)
    {
        IsRunning = _progressService.IsRunning;
        HasPercentage = _progressService.Percentage is not null;
        Percentage = _progressService.Percentage ?? 0.0f;
        Text = _progressService.Text;
    }
}
