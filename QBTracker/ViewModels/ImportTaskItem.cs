using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QBTracker.ViewModels
{
   public class ImportTaskItem(string key, string name) : INotifyPropertyChanged
   {

      public string Key  { get; } = key;

      public string Name { get; } = name;

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
