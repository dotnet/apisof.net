using System;
using System.Windows.Input;

namespace NetUpgradePlanner.Mvvm;

internal sealed class Command : ICommand
{
    private readonly Action<object?> _executeHandler;
    private readonly Func<object?, bool> _canExecuteHandler;

    public Command(Action executeHandler)
        : this(executeHandler, () => true)
    {
    }

    public Command(Action<object?> executeHandler)
    : this(executeHandler, o => true)
    {
    }

    public Command(Action executeHandler, Func<bool> canExecuteHandler)
        : this(o => executeHandler(), o => canExecuteHandler())
    {
    }

    public Command(Action<object?> executeHandler, Func<object?, bool> canExecuteHandler)
    {
        _executeHandler = executeHandler;
        _canExecuteHandler = canExecuteHandler;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecuteHandler(parameter);
    }

    public void Execute(object? parameter)
    {
        _executeHandler(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
