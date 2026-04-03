using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QBTracker.ViewModels
{
   public class ClearTasksDialogViewModel : INotifyPropertyChanged
   {
      public int KeepTopCount
      {
         get;
         set
         {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
         }
      } = 10;

      public int KeepDays
      {
         get;
         set
         {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
         }
      } = 30;

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
