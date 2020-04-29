using QBTracker.DataAccess;
using QBTracker.Model;
using QBTracker.Util;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace QBTracker.ViewModels
{
    public class MainWindowViewModel : ValidatableModel
    {
        private DateTime? _selectedDate;
        private int? _selectedProjectId;
        private int _selectedTransitionIndex;

        public IRepository Repository;
        private ProjectViewModel _createdProject;
        private Task _selectedTask;
        private int? _selectedTaskId;
        private TaskViewModel _createdTask;
        private TimeRecordViewModel _currentTimeRecord;

        public MainWindowViewModel()
        {
            Repository = new Repository();
            CreateNewProject = new RelayCommand(ExecuteCreateNewProject);
            StartStopRecording = new RelayCommand(ExecuteStartStopRecording, _ => SelectedProjectId.HasValue && SelectedTaskId.HasValue);
            CreateNewTask = new RelayCommand(ExecuteCreateNewTask, _ => SelectedProjectId != null);
            LoadProjects();
            SelectedDate = DateTime.Today;
            var tr = Repository.GetLastTimeRecord();
            if (tr != null && tr.EndTime == null)
            {
                var trVm = TimeRecords.FirstOrDefault(x => x.TimeRecord.Id == tr.Id) ?? new TimeRecordViewModel(tr, this);
                this.SelectedProjectId = tr.ProjectId;
                this.SelectedTaskId = tr.TaskId;
                this.CurrentTimeRecord = trVm;
            }
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
                LoadTimeRecords();
            }
        }

        public int? SelectedProjectId
        {
            get => _selectedProjectId;
            set
            {
                _selectedProjectId = value;
                NotifyOfPropertyChange();
                LoadTasks();
            }
        }

        public int? SelectedTaskId
        {
            get => _selectedTaskId;
            set
            {
                _selectedTaskId = value;
                NotifyOfPropertyChange();
            }
        }

        public Task SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsRecording
        {
            get => CurrentTimeRecord != null;
        }

        public bool IsNotRecording
        {
            get => CurrentTimeRecord == null;
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

        public TaskViewModel CreatedTask
        {
            get => _createdTask;
            set
            {
                _createdTask = value;
                NotifyOfPropertyChange();
            }
        }

        public ObservableRangeCollection<ProjectViewModel> Projects { get; } = new ObservableRangeCollection<ProjectViewModel>();
        public ObservableRangeCollection<TaskViewModel> Tasks { get; } = new ObservableRangeCollection<TaskViewModel>();
        public ObservableRangeCollection<TimeRecordViewModel> TimeRecords { get; } = new ObservableRangeCollection<TimeRecordViewModel>();

        public TimeRecordViewModel CurrentTimeRecord
        {
            get => _currentTimeRecord;
            set
            {
                if (_currentTimeRecord == value)
                    return;
                _currentTimeRecord = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(IsRecording));
                NotifyOfPropertyChange(nameof(IsNotRecording));
            }
        }

        public RelayCommand CreateNewProject { get; }
        private void ExecuteCreateNewProject(object obj)
        {
            CreatedProject = new ProjectViewModel(new Project(), this);
            SelectedTransitionIndex = Pages.CreateProject;
        }

        public RelayCommand StartStopRecording { get; }
        private void ExecuteStartStopRecording(object obj)
        {
            if (SelectedProjectId == null || SelectedTaskId == null)
                return;
            if (!IsRecording)
            {
                var project = Repository.GetProjectById(SelectedProjectId.Value);
                var task = Repository.GetTaskById(SelectedTaskId.Value);
                var timeRecord = new TimeRecord
                {
                    StartTime = DateTime.UtcNow,
                    ProjectName = project.Name,
                    ProjectId = project.Id,
                    TaskName = task.Name,
                    TaskId = task.Id,
                };
                Repository.AddTimeRecord(timeRecord);
                CurrentTimeRecord = new TimeRecordViewModel(timeRecord, this);
                if (SelectedDate?.Date == DateTime.Today)
                {
                    TimeRecords.Add(CurrentTimeRecord);
                }
            }
            else
            {
                var record = CurrentTimeRecord;
                CurrentTimeRecord = null;
                record.EndTime = DateTime.UtcNow;
                Repository.UpdateTimeRecord(record.TimeRecord);
            }
        }

        public RelayCommand CreateNewTask { get; }
        private void ExecuteCreateNewTask(object obj)
        {
            if (SelectedProjectId == null)
                return;
            CreatedTask = new TaskViewModel(new Task() { ProjectId = SelectedProjectId.Value }, this);
            SelectedTransitionIndex = Pages.CreateTask;
        }

        public void Show()
        {
            SelectedTransitionIndex = Pages.MainView;
        }

        private void LoadProjects()
        {
            Projects.AddRange(Repository.GetProjects()
                .Select(x => new ProjectViewModel(x, this)));
        }

        private void LoadTasks()
        {
            Tasks.Clear();
            if (SelectedProjectId.HasValue)
                Tasks.AddRange(Repository.GetTasks(SelectedProjectId.Value)
                    .Select(x => new TaskViewModel(x, this)));
        }

        private void LoadTimeRecords()
        {
            TimeRecords.Clear();
            if (SelectedDate != null)
                TimeRecords.AddRange(Repository.GetTimeRecords(SelectedDate.Value)
                    .Select(x => new TimeRecordViewModel(x, this)));
        }
    }
}