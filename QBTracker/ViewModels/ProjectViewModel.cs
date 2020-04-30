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
        private readonly MainWindowViewModel mainVm;

        public ProjectViewModel(Project project, MainWindowViewModel mainWindowViewModel)
        {
            mainVm = mainWindowViewModel;
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

        public IEnumerable<ProjectViewModel> Projects => mainVm.Projects;

        public RelayCommand Save { get; }

        public Action OnSave { get; set; }

        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            mainVm.Repository.AddProject(Project);
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
            mainVm.CreatedProject = null;
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
                this.Project.IsDeleted = true;
                this.mainVm.Repository.UpdateProject(this.Project);
                this.mainVm.Projects.Remove(this);
            }
        }
    }
}