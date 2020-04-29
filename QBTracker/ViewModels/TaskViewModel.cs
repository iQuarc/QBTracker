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

        public IEnumerable<TaskViewModel> Tasks => _mainWindowViewModel.Tasks;

        public RelayCommand Save { get; }

        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            _mainWindowViewModel.Repository.AddTask(Task);
            _mainWindowViewModel.Tasks.Add(this);
            _mainWindowViewModel.Show();
            _mainWindowViewModel.SelectedTaskId = this.Task.Id;
        }

        private bool CanExecuteSave(object o)
        {
            Validate();
            return !HasErrors;
        }

        public RelayCommand GoBack { get; }
        private void ExecuteGoBack(object o)
        {
            _mainWindowViewModel.CreatedProject = null;
            _mainWindowViewModel.Show();
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