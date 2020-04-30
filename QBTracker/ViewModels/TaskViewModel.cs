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
        private readonly MainWindowViewModel mainVm;

        public TaskViewModel(Task task, MainWindowViewModel mainWindowViewModel)
        {
            mainVm = mainWindowViewModel;
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

        public IEnumerable<TaskViewModel> Tasks => mainVm.Tasks;

        public RelayCommand Save { get; }
        public Action OnSave { get; set; }
        public Action<TaskViewModel> OnRemove { get; set; }
        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            mainVm.Repository.AddTask(Task);
            mainVm.GoBack();
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
            mainVm.CreatedTask = null;
            mainVm.GoBack();
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
                this.mainVm.Repository.UpdateTask(this.Task);
                this.mainVm.Tasks.Remove(this);
                this.OnRemove?.Invoke(this);
            }
        }
    }
}