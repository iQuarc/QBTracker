using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
   public class ImportDialogViewModel : INotifyPropertyChanged
   {
      public ImportDialogViewModel(string title, IEnumerable<(string Key, string Name)> taskNames)
      {
         Title = title;
         Items = new ObservableRangeCollection<ImportTaskItem>(
            taskNames.Select(n => new ImportTaskItem(n.Key, n.Name)));
         SelectAllCommand = new RelayCommand(_ => SetAll(true));
         SelectNoneCommand = new RelayCommand(_ => SetAll(false));
         this.GroupImport = true;
      }

      public string Title { get; }
      public ObservableRangeCollection<ImportTaskItem> Items { get; }
      public ICommand SelectAllCommand { get; }
      public ICommand SelectNoneCommand { get; }

      public bool GroupImport
      {
         get;
         set
         {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
         }
      }

      public IEnumerable<ImportTaskItem> Selected =>
         Items.Where(i => i.IsSelected);

      private void SetAll(bool selected)
      {
         foreach (var item in Items)
            item.IsSelected = selected;
      }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string propertyName = "")
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
