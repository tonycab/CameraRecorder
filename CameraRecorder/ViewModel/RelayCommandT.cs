
using System;
using System.Windows.Input;

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;         // ce que la commande fait
    private readonly Func<T, bool>? _canExecute; // optionnel : peut exécuter ?

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null) return true;

        // convertit le paramètre en T
        if (parameter == null && typeof(T).IsValueType) return _canExecute(default!);
        return parameter is T t && _canExecute(t);
    }

    public void Execute(object? parameter)
    {
        if (parameter == null && typeof(T).IsValueType)
        {
            _execute(default!);
        }
        else if (parameter is T t)
        {
            _execute(t);
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}


