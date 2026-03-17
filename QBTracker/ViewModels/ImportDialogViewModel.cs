using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
   public class ImportDialogViewModel
   {
      public ImportDialogViewModel(string title, IEnumerable<string> taskNames)
      {
         Title = title;
         Items = new ObservableRangeCollection<ImportTaskItem>(
            taskNames.Select(n => new ImportTaskItem(n)));
         SelectAllCommand = new RelayCommand(_ => SetAll(true));
         SelectNoneCommand = new RelayCommand(_ => SetAll(false));
      }

      public string Title { get; }
      public ObservableRangeCollection<ImportTaskItem> Items { get; }
      public ICommand SelectAllCommand { get; }
      public ICommand SelectNoneCommand { get; }

      public IEnumerable<string> SelectedNames =>
         Items.Where(i => i.IsSelected).Select(i => i.Name);

      private void SetAll(bool selected)
      {
         foreach (var item in Items)
            item.IsSelected = selected;
      }
   }
}
