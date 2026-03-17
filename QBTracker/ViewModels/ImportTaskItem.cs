using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QBTracker.ViewModels
{
   public class ImportTaskItem : INotifyPropertyChanged
   {
      public ImportTaskItem(string name)
      {
         Name = name;
      }

      public string Name { get; }

      public bool IsSelected
      {
         get;
         set
         {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
         }
      } = true;

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
