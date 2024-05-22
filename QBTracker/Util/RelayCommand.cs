using System;
using System.Windows.Input;

namespace QBTracker.Util
{
    public class RelayCommand : ICommand
    {
        readonly Action<object> _execute = null;
        readonly Func<object, bool> _canExecute = null;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void RaiseCanExecuteChanged()
        {
            localHandler?.Invoke(this, EventArgs.Empty);
        }

        public EventHandler localHandler;
        public event EventHandler CanExecuteChanged
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

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
