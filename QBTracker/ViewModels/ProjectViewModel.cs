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
    public class ProjectViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        public ProjectViewModel(Project project, MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            Save = new RelayCommand(ExecuteSave, CanExecuteSave);
            GoBack = new RelayCommand(ExecuteGoBack);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            Project = project;
            ClearTouched();
        }

        public Project Project { get; }

        [Required]
        [DisplayName("Project Name")]
        [StringLength(40)]
        public string Name
        {
            get => Project.Name;
            set
            {
                Project.Name = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsFocused { get; set; } = true;

        public IEnumerable<ProjectViewModel> Projects => _mainWindowViewModel.Projects;

        public RelayCommand Save { get; }

        public Action OnSave { get; set; }

        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            _mainWindowViewModel.Repository.AddProject(Project);
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
            _mainWindowViewModel.CreatedProject = null;
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
                this.Project.IsDeleted = true;
                this._mainWindowViewModel.Repository.UpdateProject(this.Project);
                this._mainWindowViewModel.Projects.Remove(this);
            }
        }
    }
}