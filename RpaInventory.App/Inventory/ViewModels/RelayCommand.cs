using System.Windows.Input;

namespace RpaInventory.App.Inventory.ViewModels;

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
        => TryGetParameter(parameter, out var typedParameter) && (_canExecute?.Invoke(typedParameter) ?? true);

    public void Execute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var typedParameter))
            return;

        _execute(typedParameter);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static bool TryGetParameter(object? parameter, out T value)
    {
        if (parameter is T typedParameter)
        {
            value = typedParameter;
            return true;
        }

        if (parameter is null && default(T) is null)
        {
            value = default!;
            return true;
        }

        value = default!;
        return false;
    }
}
