﻿using MaterialDesignThemes.Wpf;

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

        public IEnumerable<ProjectViewModel> Projects => _mainWindowViewModel.Projects;

        public RelayCommand Save { get; }

        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            _mainWindowViewModel.Repository.AddProject(Project);
            _mainWindowViewModel.Projects.Add(this);
            _mainWindowViewModel.Show();
            _mainWindowViewModel.SelectedProjectId = this.Project.Id;
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
                this.Project.IsDeleted = true;
                this._mainWindowViewModel.Repository.UpdateProject(this.Project);
                this._mainWindowViewModel.Projects.Remove(this);
            }
        }
    }
}