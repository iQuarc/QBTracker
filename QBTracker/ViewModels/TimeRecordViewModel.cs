using Humanizer;

using MaterialDesignThemes.Wpf;

using QBTracker.Model;
using QBTracker.Util;
using QBTracker.Views;

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using QBTracker.DataAccess;

namespace QBTracker.ViewModels
{
    public class TimeRecordViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel mainVm;
        private readonly DispatcherTimer timer;
        private ObservableRangeCollection<TaskViewModel> tasks;

        public TimeRecordViewModel(TimeRecord timeRecord, MainWindowViewModel mainWindowViewModel)
        {
            mainVm = mainWindowViewModel;
            CreateNewProject = new RelayCommand(ExecuteCreateNewProject);
            CreateNewTask = new RelayCommand(ExecuteCreateNewTask, _ => SelectedProjectId != null);
            TimeRecord = timeRecord;
            DeleteCommand = new RelayCommand(ExecuteDelete);
            EditCommand = new RelayCommand(ExecuteEdit);
            GoBack = new RelayCommand(ExecuteGoBack);

            if (!EndTime.HasValue)
            {
                timer = new DispatcherTimer();
                timer.Tick += TimerOnTick;
                timer.Interval += TimeSpan.FromSeconds(1);
                timer.Start();
            }
            LoadTasks();
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            NotifyOfPropertyChange(nameof(Duration));
        }

        public TimeRecord TimeRecord { get; }

        public MainWindowViewModel MainVm => mainVm;

        public DateTime StarTime
        {
            get => TimeRecord.StartTime.ToLocalTime();
            set
            {
                TimeRecord.StartTime = value.ToUniversalTime();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
            }
        }

        public DateTime? EndTime
        {
            get => TimeRecord.EndTime;
            set
            {
                if (value == null)
                    return;
                TimeRecord.EndTime = value.Value.ToUniversalTime();
                timer?.Stop();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
                NotifyOfPropertyChange(nameof(EndTime));
            }
        }

        public bool IsEndTimeEnabled => EndTime != null;

        public RelayCommand GoBack { get; }
        private void ExecuteGoBack(object o)
        {
            mainVm.CreatedTask = null;
            mainVm.GoBack();
        }

        public int? SelectedProjectId
        {
            get => TimeRecord.ProjectId;
            set
            {
                if (value == null || value.Value == TimeRecord.ProjectId)
                {
                    NotifyOfPropertyChange();
                    return;
                }
                TimeRecord.ProjectId = value.Value;
                NotifyOfPropertyChange();
                LoadTasks();
                var project = mainVm.Repository.GetProjectById(TimeRecord.ProjectId);
                TimeRecord.ProjectName = project.Name;
                mainVm.Repository.UpdateTimeRecord(TimeRecord);
                NotifyOfPropertyChange(nameof(TimeRecord));
            }
        }

        public int? SelectedTaskId
        {
            get => TimeRecord.TaskId;
            set
            {
                if (value == null || value.Value == TimeRecord.TaskId)
                {
                    NotifyOfPropertyChange();
                    return;
                }
                TimeRecord.TaskId = value.Value;
                NotifyOfPropertyChange();
                var task = mainVm.Repository.GetTaskById(TimeRecord.TaskId);
                TimeRecord.TaskName = task.Name;
                mainVm.Repository.UpdateTimeRecord(TimeRecord);
                NotifyOfPropertyChange(nameof(TimeRecord));
            }
        }

        public string DurationText => Duration.Humanize();
        public TimeSpan Duration => EndTime.HasValue
            ? (EndTime.Value - StarTime)
            : (DateTime.Now - StarTime);

        public ObservableRangeCollection<TaskViewModel> Tasks
        {
            get
            {
                if (tasks == null)
                {
                    tasks = new ObservableRangeCollection<TaskViewModel>();
                    LoadTasks();
                }
                return tasks;
            }
        }

        public RelayCommand DeleteCommand { get; }
        private async void ExecuteDelete(object o)
        {
            if ((bool)await DialogHost.Show(new ConfirmDialog
            {
                DataContext = "Are you sure?"
            }))
            {
                this.mainVm.Repository.DeleteTimeRecord(this.TimeRecord.Id);
                this.mainVm.TimeRecords.Remove(this);
                this.mainVm.GoBack();
            }
        }

        public RelayCommand EditCommand { get; }
        private void ExecuteEdit(object o)
        {
            mainVm.TimeRecordInEdit = this;
            mainVm.SelectedTransitionIndex = Pages.EditTimeRecord;
        }

        public RelayCommand CreateNewProject { get; }
        private void ExecuteCreateNewProject(object obj)
        {
            mainVm.CreatedProject = new ProjectViewModel(new Project(), this.mainVm);
            mainVm.CreatedProject.OnSave = () =>
            {
                this.SelectedProjectId = MainVm.CreatedProject.Project.Id;
                this.MainVm.Projects.Add(MainVm.CreatedProject);
            };
            MainVm.SelectedTransitionIndex = Pages.CreateProject;
        }

        public RelayCommand CreateNewTask { get; }
        private void ExecuteCreateNewTask(object obj)
        {
            if (SelectedProjectId == null)
                return;
            mainVm.CreatedTask = new TaskViewModel(new Task { ProjectId = SelectedProjectId.Value }, this.MainVm);
            mainVm.CreatedTask.OnSave = () =>
            {
                this.SelectedTaskId = mainVm.CreatedTask.Task.Id;
                this.Tasks.Add(mainVm.CreatedTask);
                if (mainVm.SelectedProjectId == mainVm.CreatedTask.Task.ProjectId)
                {
                    mainVm.Tasks.Add(mainVm.CreatedTask);
                }
            };
            MainVm.SelectedTransitionIndex = Pages.CreateTask;
        }

        private void LoadTasks()
        {
            Tasks.Clear();
            if (SelectedProjectId.HasValue)
                Tasks.AddRange(mainVm.Repository.GetTasks(SelectedProjectId.Value)
                    .Select(x => new TaskViewModel(x, this.mainVm)));
        }
    }
}
