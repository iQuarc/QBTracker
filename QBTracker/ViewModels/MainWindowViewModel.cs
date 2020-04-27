using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices;

using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class MainWindowViewModel : ValidatableModel
    {
        private DateTime? _selectedDate;
        private Project _selectedProject;

        public MainWindowViewModel()
        {
            Projects = new List<Project>{new Project(){Id = 1, Name = "SP"}};
            SelectedDate = DateTime.Today;
        }

        [Required]
        [DisplayName("Selected Date")]
        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                _selectedDate = value;
                NotifyOfPropertyChange();
            }
        }

        public Project SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<Project> Projects { get; }
    }

    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
