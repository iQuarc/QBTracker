using System.Windows;
using System.Windows.Input;
using QBTracker.DataAccess;

namespace QBTracker.Util
{
    public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
       : ICommand
    {
        readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public bool CanExecute(object? parameter)
        {
            try
            {
                return canExecute?.Invoke(parameter) ?? true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
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
            try
            {
                _execute(parameter);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private static void LogError(Exception ex)
        {
            if (Application.Current?.Resources["Repository"] is ILogger logger)
                logger.Error($"RelayCommand error: {ex}");
        }
    }
}
