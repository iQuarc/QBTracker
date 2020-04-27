using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using QBTracker.Model;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class ProjectViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        public ProjectViewModel(Project project, MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            Save = new RelayCommand(ExecuteSave, CanExecuteSave);
            Project = project;
        }

        public Project Project { get; }

        [Required]
        [DisplayName("Project Name")]
        public string Name
        {
            get => Project.Name;
            set
            {
                Project.Name = value;
                NotifyOfPropertyChange();
            }
        }

        public RelayCommand Save { get; }

        private void ExecuteSave(object o)
        {
            Validate();
            if (HasErrors)
                return;
            _mainWindowViewModel.SelectedProject = null;
            _mainWindowViewModel.Repository.AddProject(Project);
            _mainWindowViewModel.Show();
            _mainWindowViewModel.SelectedProject = _mainWindowViewModel.Projects.FirstOrDefault(x => x.Project.Id == Project.Id);
        }

        private bool CanExecuteSave(object o)
        {
            Validate();
            return !HasErrors;
        }
    }
}