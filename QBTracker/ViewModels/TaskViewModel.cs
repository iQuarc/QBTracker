using System;
using MaterialDesignThemes.Wpf;

using QBTracker.Model;
using QBTracker.Util;
using QBTracker.Views;

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QBTracker.ViewModels
{
    public class TaskViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        public TaskViewModel(Task task, MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            Save = new RelayCommand(ExecuteSave, CanExecuteSave);
            GoBack = new RelayCommand(ExecuteGoBack);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            Task = task;
            ClearTouched();
        }

        public Task Task { get; }

        [Required]
        [DisplayName("Task Name")]
        [StringLength(400)]
        public string Name
        {
            get => Task.Name;
            set
            {
                Task.Name = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsFocused { get; set; } = true;

        public IEnumerable<TaskViewModel> Tasks => _mainWindowViewModel.Tasks;

        public RelayCommand Save { get; }
        public Action OnSave { get; set; }
        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            _mainWindowViewModel.Repository.AddTask(Task);
            _mainWindowViewModel.GoBack();
            OnSave?.Invoke();
        }

        private bool CanExecuteSave(object o)
        {
            Validate();
            return !HasErrors;
        }

        public RelayCommand GoBack { get; }
        private void ExecuteGoBack(object o)
        {
            _mainWindowViewModel.CreatedTask = null;
            _mainWindowViewModel.GoBack();
        }

        public RelayCommand DeleteCommand { get; }
        private async void ExecuteDelete(object o)
        {
            if ((bool)await DialogHost.Show(new ConfirmDialog
            {
                DataContext = "Are you sure?"
            }))
            {
                this.Task.IsDeleted = true;
                this._mainWindowViewModel.Repository.UpdateTask(this.Task);
                this._mainWindowViewModel.Tasks.Remove(this);
            }
        }
    }
}