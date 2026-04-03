using System.Windows;
using QBTracker.DataAccess;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using QBTracker.Views;

namespace QBTracker.Util
{
   public class AsyncRelayCommand(Func<object?, ValueTask> executeAsync, Func<object?, bool>? canExecute = null)
      : ICommand
   {
      readonly Func<object?, ValueTask> _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));

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
            localHandler                    += value;
         }
         remove
         {
            CommandManager.RequerySuggested -= value;
            localHandler                    -= value;
         }
      }

      public async void Execute(object? parameter)
      {
         try
         {
            await _executeAsync(parameter);
         }
         catch (Exception ex)
         {
            LogError(ex);
            await DialogHost.Show(new ConfirmDialog
            {
               DataContext = $"Error: {ex.Message}"
            });
         }
      }


      private static void LogError(Exception ex)
      {
         if (Application.Current?.Resources["Repository"] is ILogger logger)
            logger.Error($"RelayCommand error: {ex}");
      }
   }
}