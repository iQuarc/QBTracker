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
      private bool _userGroupImport = true;
      private bool _isUpdatingGroupImport;

      public ImportDialogViewModel(string title, IEnumerable<(string Key, string Name)> taskNames)
      {
         Title = title;
         Items = new ObservableRangeCollection<ImportTaskItem>(
            taskNames.Select(n => new ImportTaskItem(n.Key, n.Name)));
         SelectAllCommand = new RelayCommand(_ => SetAll(true));
         SelectNoneCommand = new RelayCommand(_ => SetAll(false));

         foreach (var item in Items)
            item.PropertyChanged += OnItemPropertyChanged;

         GroupImport = true;
         UpdateGroupImportState();
      }

      public string Title { get; }
      public ObservableRangeCollection<ImportTaskItem> Items { get; }
      public ICommand SelectAllCommand { get; }
      public ICommand SelectNoneCommand { get; }

      public bool IsGroupImportEnabled
      {
         get;
         private set
         {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
         }
      }

      public bool GroupImport
      {
         get;
         set
         {
            if (field == value) return;
            field = value;
            if (!_isUpdatingGroupImport)
               _userGroupImport = value;
            OnPropertyChanged();
         }
      }

      public IEnumerable<ImportTaskItem> Selected =>
         Items.Where(i => i.IsSelected);

      private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == nameof(ImportTaskItem.IsSelected))
            UpdateGroupImportState();
      }

      private void UpdateGroupImportState()
      {
         var selectedCount = Items.Count(i => i.IsSelected);
         _isUpdatingGroupImport = true;
         try
         {
            if (selectedCount >= 2)
            {
               IsGroupImportEnabled = true;
               GroupImport = _userGroupImport;
            }
            else
            {
               IsGroupImportEnabled = false;
               GroupImport          = false;
            }
         }
         finally
         {
            _isUpdatingGroupImport = false;
         }
      }

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
