using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using QBTracker.DataAccess;
using QBTracker.Model;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class MainWindowViewModel : ValidatableModel
    {
        private DateTime? _selectedDate;
        private ProjectViewModel _selectedProject;
        private int _selectedTransitionIndex;

        public IRepository Repository;
        private bool _isRecording;
        private ProjectViewModel _createdProject;

        public MainWindowViewModel()
        {
            CreateNewProject = new RelayCommand(ExecuteCreateNewProject);
            StartStopRecording = new RelayCommand(ExecuteStartStopRecording);
            SelectedDate = DateTime.Today;
            Repository = new Repository();
            LoadProjects();
        }

        public int SelectedTransitionIndex
        {
            get => _selectedTransitionIndex;
            set
            {
                _selectedTransitionIndex = value;
                NotifyOfPropertyChange();
            }
        }

        [Required]
        [DisplayName("Selected Date")]
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                NotifyOfPropertyChange();
            }
        }

        public ProjectViewModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
                NotifyOfPropertyChange();
            }
        }

        public ProjectViewModel CreatedProject
        {
            get => _createdProject;
            set
            {
                _createdProject = value; 
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<ProjectViewModel> Projects { get; set; }
        

        public RelayCommand CreateNewProject { get; }
        private void ExecuteCreateNewProject(object obj)
        {
            CreatedProject = new ProjectViewModel(new Project(), this);
            SelectedTransitionIndex = Pages.CreateProject;
        }

        public RelayCommand StartStopRecording { get; }
        private void ExecuteStartStopRecording(object obj)
        {
            IsRecording = !IsRecording;
        }

        public void Show()
        {
            SelectedTransitionIndex = Pages.MainView;
            LoadProjects();
        }

        private void LoadProjects()
        {
            Projects = Repository.GetProjects()
                .Select(x => new ProjectViewModel(x, this));
            NotifyOfPropertyChange(nameof(Projects));
        }
    }
}