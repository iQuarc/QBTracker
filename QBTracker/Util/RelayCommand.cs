using System;
using System.Windows.Input;

namespace QBTracker.Util
{
    public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
       : ICommand
    {
        readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void RaiseCanExecuteChanged()
        {
            localHandler?.Invoke(this, EventArgs.Empty);
        }

        private EventHandler? localHandler;
        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                localHandler += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                localHandler -= value;
            }
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
