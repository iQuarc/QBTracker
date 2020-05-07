using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;

using QBTracker.AutomaticUpdader;
using QBTracker.DataAccess;
using QBTracker.Model;
using QBTracker.Util;

using Task = QBTracker.Model.Task;

namespace QBTracker.ViewModels
{
    public class MainWindowViewModel : ValidatableModel
    {
        private readonly Stack<int> navigationHistory = new Stack<int>();
        public readonly IRepository Repository;
        private ProjectViewModel _createdProject;
        private TaskViewModel _createdTask;
        private TimeRecordViewModel _currentTimeRecord;
        private DateTime _selectedDate;
        private int? _selectedProjectId;
        private int? _selectedTaskId;
        private int _selectedTransitionIndex;
        private TimeRecordViewModel _timeRecordInEdit;
        private TimeSpan? closedDayDuration;
        private TimeSpan? totalDayDuration;
        private readonly DispatcherTimer timer;

        public MainWindowViewModel()
        {
            Repository = new Repository();
            CreateNewProject = new RelayCommand(ExecuteCreateNewProject);
            CreateNewTask = new RelayCommand(ExecuteCreateNewTask, _ => SelectedProjectId != null);
            StartStopRecording = new RelayCommand(ExecuteStartStopRecording,
                _ => SelectedProjectId.HasValue && SelectedTaskId.HasValue);
            DateStepBack = new RelayCommand(ExecuteDateStepBack);
            DateStepForward = new RelayCommand(ExecuteDateStepForward);
            SelectToday = new RelayCommand(_ => SelectedDate = DateTime.Today, _ => SelectedDate != DateTime.Today);
            ExportCommand = new RelayCommand(_ => SelectedTransitionIndex = Pages.ExportToExcel);
            SettingsCommand = new RelayCommand(_ => SelectedTransitionIndex = Pages.Settings);
            ExportViewModel = new ExportViewModel(this);
            SettingsViewModel = new SettingsViewModel(this);
            LoadProjects();
            SelectedDate = DateTime.Today;
            var tr = Repository.GetLastTimeRecord();

            timer = new DispatcherTimer();
            timer.Tick += TimerOnTick;
            timer.Interval += TimeSpan.FromSeconds(1);

            if (tr != null && tr.EndTime == null)
            {
                var trVm = TimeRecords.FirstOrDefault(x => x.TimeRecord.Id == tr.Id) ??
                           new TimeRecordViewModel(tr, this);
                SelectedProjectId = tr.ProjectId;
                SelectedTaskId = tr.TaskId;
                CurrentTimeRecord = trVm;
            }

            _ = SettingsViewModel.CheckForUpdateSequence();
        }

        public string Title => $"QBTracker {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";



        private void TimerOnTick(object sender, EventArgs e)
        {
            CurrentTimeRecord.NotifyOfPropertyChange(nameof(Duration));
            AddDayDuration(CurrentTimeRecord.Duration);
        }

        public int SelectedTransitionIndex
        {
            get => _selectedTransitionIndex;
            set
            {
                if (_selectedTransitionIndex == value)
                    return;
                navigationHistory.Push(_selectedTransitionIndex);
                _selectedTransitionIndex = value;
                NotifyOfPropertyChange();
            }
        }

        [Required]
        [DisplayName("Selected Date")]
        public DateTime SelectedDate
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
                if (CurrentTimeRecord != null)
                {
                    timer.Start();
                }
                else
                {
                    timer.Stop();
                }
            }
        }

        public string VersionString => $"QBTracker {Assembly.GetExecutingAssembly().GetName().Version}";

        public RelayCommand CreateNewProject { get; }
        public RelayCommand CreateNewTask { get; }
        public RelayCommand StartStopRecording { get; }
        public RelayCommand DateStepBack { get; }
        public RelayCommand DateStepForward { get; }
        public RelayCommand SelectToday { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand SettingsCommand { get; }

        public TimeRecordViewModel TimeRecordInEdit
        {
            get => _timeRecordInEdit;
            set
            {
                _timeRecordInEdit = value;
                NotifyOfPropertyChange();
            }
        }

        public ExportViewModel ExportViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        private void ExecuteDateStepBack(object obj)
        {
            SelectedDate = SelectedDate.AddDays(-1);
        }

        private void ExecuteDateStepForward(object obj)
        {
            SelectedDate = SelectedDate.AddDays(1);
        }

        public void GoBack()
        {
            if (navigationHistory.TryPop(out var index))
                _selectedTransitionIndex = index;
            else
                _selectedTransitionIndex = Pages.MainView;
            NotifyOfPropertyChange(nameof(SelectedTransitionIndex));
            if (SelectedTransitionIndex == Pages.MainView)
                LoadTimeRecords();
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
            CreatedTask = new TaskViewModel(new Task { ProjectId = SelectedProjectId.Value }, this);
            CreatedTask.OnSave = () =>
            {
                Tasks.Insert(0, CreatedTask);
                SelectedTaskId = CreatedTask.Task.Id;
            };
            CreatedTask.OnRemove = x => Tasks.Remove(x);
            SelectedTransitionIndex = Pages.CreateTask;
        }

        private void ExecuteStartStopRecording(object obj)
        {
            if (SelectedProjectId == null || SelectedTaskId == null)
                return;
            if (!IsRecording)
            {
                if (SelectedDate.Date != DateTime.Today)
                    SelectedDate = DateTime.Today;
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
                TimeRecords.Add(CurrentTimeRecord);
            }
            else
            {
                var record = CurrentTimeRecord;
                CurrentTimeRecord = null;
                record.EndTime = DateTime.Now;
                Repository.UpdateTimeRecord(record.TimeRecord);
                record.NotifyOfPropertyChange();
            }
        }


        public TimeSpan? TotalDayDuration
        {
            get => totalDayDuration;
            private set
            {
                totalDayDuration = value;
                NotifyOfPropertyChange();
            }
        }

        public void AddDayDuration(TimeSpan duration)
        {
            if (SelectedDate == DateTime.Today)
            {
                TotalDayDuration = (closedDayDuration ?? TimeSpan.Zero) + duration;
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
            closedDayDuration = null;
            TotalDayDuration = null;
            TimeRecords.Clear();
            TimeRecords.AddRange(Repository.GetTimeRecords(SelectedDate)
                .Select(x =>
                {
                    if (x.EndTime != null)
                    {
                        closedDayDuration = (closedDayDuration ?? TimeSpan.Zero) + (x.EndTime - x.StartTime);
                    }
                    if (CurrentTimeRecord?.TimeRecord?.Id == x.Id)
                        return CurrentTimeRecord;
                    return new TimeRecordViewModel(x, this);
                }));
            TotalDayDuration = closedDayDuration;
        }
    }
}