using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using MaterialDesignThemes.Wpf.Transitions;
using QBTracker.DataAccess;
using QBTracker.Model;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class MainWindowViewModel : ValidatableModel
    {
        private readonly Stack<int> NavigationHistory = new Stack<int>();
        private ProjectViewModel _createdProject;
        private TaskViewModel _createdTask;
        private TimeRecordViewModel _currentTimeRecord;
        private DateTime? _selectedDate;
        private int? _selectedProjectId;
        private int? _selectedTaskId;
        private int _selectedTransitionIndex;
        private TimeRecordViewModel _timeRecordInEdit;

        public readonly IRepository Repository;

        public MainWindowViewModel()
        {
            Repository = new Repository();
            CreateNewProject = new RelayCommand(ExecuteCreateNewProject);
            CreateNewTask = new RelayCommand(ExecuteCreateNewTask, _ => SelectedProjectId != null);
            StartStopRecording = new RelayCommand(ExecuteStartStopRecording,
                _ => SelectedProjectId.HasValue && SelectedTaskId.HasValue);
            LoadProjects();
            SelectedDate = DateTime.Today;
            var tr = Repository.GetLastTimeRecord();
            if (tr != null && tr.EndTime == null)
            {
                var trVm = TimeRecords.FirstOrDefault(x => x.TimeRecord.Id == tr.Id) ??
                           new TimeRecordViewModel(tr, this);
                SelectedProjectId = tr.ProjectId;
                SelectedTaskId = tr.TaskId;
                CurrentTimeRecord = trVm;
            }
        }

        public int SelectedTransitionIndex
        {
            get => _selectedTransitionIndex;
            set
            {
                if (_selectedTransitionIndex == value)
                    return;
                NavigationHistory.Push(_selectedTransitionIndex);
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

        public bool IsRecording => CurrentTimeRecord != null;

        public bool IsNotRecording => CurrentTimeRecord == null;

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

        public ObservableRangeCollection<ProjectViewModel> Projects { get; } =
            new ObservableRangeCollection<ProjectViewModel>();

        public ObservableRangeCollection<TaskViewModel> Tasks { get; } = new ObservableRangeCollection<TaskViewModel>();

        public ObservableRangeCollection<TimeRecordViewModel> TimeRecords { get; } =
            new ObservableRangeCollection<TimeRecordViewModel>();

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

        public RelayCommand CreateNewTask { get; }

        public RelayCommand StartStopRecording { get; }

        public TimeRecordViewModel TimeRecordInEdit
        {
            get => _timeRecordInEdit;
            set
            {
                _timeRecordInEdit = value;
                NotifyOfPropertyChange();
            }
        }


        public void GoBack()
        {
            if (NavigationHistory.TryPop(out var index))
                _selectedTransitionIndex = index;
            else
                _selectedTransitionIndex = Pages.MainView;

            NotifyOfPropertyChange(nameof(SelectedTransitionIndex));
        }

        private void ExecuteCreateNewProject(object obj)
        {
            CreatedProject = new ProjectViewModel(new Project(), this);
            CreatedProject.OnSave = () =>
            {
                Projects.Add(CreatedProject);
                SelectedProjectId = CreatedProject.Project.Id;
            };
            SelectedTransitionIndex = Pages.CreateProject;
        }

        private void ExecuteCreateNewTask(object obj)
        {
            if (SelectedProjectId == null)
                return;
            CreatedTask = new TaskViewModel(new Task {ProjectId = SelectedProjectId.Value}, this);
            CreatedTask.OnSave = () =>
            {
                Tasks.Add(CreatedTask);
                SelectedTaskId = CreatedTask.Task.Id;
            };
            SelectedTransitionIndex = Pages.CreateTask;
        }

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
                    TaskId = task.Id
                };
                Repository.AddTimeRecord(timeRecord);
                CurrentTimeRecord = new TimeRecordViewModel(timeRecord, this);
                if (SelectedDate?.Date == DateTime.Today) TimeRecords.Add(CurrentTimeRecord);
            }
            else
            {
                var record = CurrentTimeRecord;
                CurrentTimeRecord = null;
                record.EndTime = DateTime.UtcNow;
                Repository.UpdateTimeRecord(record.TimeRecord);
            }
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
                    .Select(x =>
                    {
                        if (CurrentTimeRecord?.TimeRecord?.Id == x.Id)
                            return CurrentTimeRecord;
                        return new TimeRecordViewModel(x, this);
                    }));
        }
    }
}