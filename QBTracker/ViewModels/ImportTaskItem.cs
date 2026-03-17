using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QBTracker.ViewModels
{
   public class ImportTaskItem : INotifyPropertyChanged
   {
      private bool isSelected = true;

      public ImportTaskItem(string name)
      {
         Name = name;
      }

      public string Name { get; }

      public bool IsSelected
      {
         get => isSelected;
         set
         {
            if (isSelected == value) return;
            isSelected = value;
            OnPropertyChanged();
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
