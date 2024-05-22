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
            SnapToPrevious = new RelayCommand(ExecuteSnapToPrevious, CanExecuteSnapToPrevious);
            if (!EndTime.HasValue)
            {

            }
        }

        public TimeRecord TimeRecord { get; }

        public MainWindowViewModel MainVm => mainVm;

        public DateTime StartTime
        {
            get => TimeRecord.StartTime.ToLocalTime();
            set
            {
                TimeRecord.StartTime = value.ToUniversalTime();
                MainVm.Repository.UpdateTimeRecord(this.TimeRecord);
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
                NotifyOfPropertyChange(nameof(SnapTooltip));
                NotifyOfPropertyChange(nameof(SnapToPrevious));
                SnapToPrevious.RaiseCanExecuteChanged();
            }
        }

        public DateTime? EndTime
        {
            get => TimeRecord.EndTime?.ToLocalTime();
            set
            {
                if (value == null)
                    return;
                TimeRecord.EndTime = value.Value.ToUniversalTime();
                MainVm.Repository.UpdateTimeRecord(this.TimeRecord);
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(TimeRecord));
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
                NotifyOfPropertyChange(nameof(IsEndTimeEnabled));
                NotifyOfPropertyChange(nameof(IsRecordingTime));
            }
        }

        public bool IsEndTimeEnabled => EndTime != null;
        public bool IsRecordingTime => EndTime == null;
        public bool IsError => EndTime != null && EndTime < StartTime;

        public RelayCommand GoBack { get; }
        public RelayCommand SnapToPrevious { get; }

        private void ExecuteGoBack(object o)
        {
            tasks = null;
            mainVm.GoBack();
        }

        private void ExecuteSnapToPrevious(object o)
        {
            this.StartTime = PreviousRecord.EndTime.Value.ToLocalTime();
        }

        private bool CanExecuteSnapToPrevious(object o)
        {          
            return PreviousRecord != null && PreviousRecord?.EndTime != TimeRecord.StartTime;
        }

        private TimeRecord? PreviousRecord =>
            mainVm.TimeRecords
                .Where(x => x.TimeRecord != this.TimeRecord && x.TimeRecord.StartTime < this.TimeRecord.StartTime && x.TimeRecord.EndTime.HasValue)
                .MaxBy(x => x.TimeRecord.StartTime)?.TimeRecord;

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

        public string Notes
        {
            get => TimeRecord.Notes;
            set
            {
                if (TimeRecord.Notes == value) 
                    return;
                TimeRecord.Notes = value;
                MainVm.Repository.UpdateTimeRecord(TimeRecord);
                NotifyOfPropertyChange();
            }
        }

        public string DurationText => EndTime.HasValue ? Duration.ToString(@"h\h\ m\m") : "...";
        public TimeSpan Duration => EndTime.HasValue
            ? (EndTime.Value - StartTime)
            : (DateTime.Now - StartTime);

        public string ToolTip
        {
            get
            {
                if (EndTime == null)
                    if (StartTime.Date == DateTime.Today)
                        return $"St: {StartTime:HH:mm}";
                    else
                        return $"St: {StartTime:F}";
                if (StartTime.Date == DateTime.Today && EndTime.Value.Date == DateTime.Today)
                    return $"St: {StartTime:HH:mm} - Et: {EndTime:HH:mm}";
                return $"St: {StartTime:F} - Et: {EndTime:F}";
            }
        }

        public string SnapTooltip
        {
            get
            {
                var rec = PreviousRecord;
                if (rec != null)
                    return $"Snap to previous: {rec.EndTime?.ToLocalTime():HH:mm}";
                return "Snap to previous";
            }
        }

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
            private set 
            { 
                tasks = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(SelectedTaskId));
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
                this.mainVm.Repository.DeleteTimeRecord(this.TimeRecord);
                this.mainVm.TimeRecords.Remove(this);
                if (!this.EndTime.HasValue)
                    this.mainVm.StartStopRecording.Execute(null);
                this.mainVm.GoBack();
            }
        }

        public RelayCommand EditCommand { get; }
        private void ExecuteEdit(object o)
        {
            mainVm.TimeRecordInEdit = this;
            mainVm.TimeRecordInEdit.Tasks = null;
            mainVm.SelectedTransitionIndex = Pages.EditTimeRecord;
        }

        public RelayCommand CreateNewProject { get; }
        private void ExecuteCreateNewProject(object obj)
        {
            mainVm.CreatedProject = new ProjectViewModel(new Project(), this.mainVm);
            mainVm.CreatedProject.OnSave = () =>
            {
                this.MainVm.Projects.Add(MainVm.CreatedProject);
                this.SelectedProjectId = MainVm.CreatedProject.Project.Id;
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
                this.Tasks.Insert(0, mainVm.CreatedTask);
                this.SelectedTaskId = mainVm.CreatedTask.Task.Id;
                if (mainVm.SelectedProjectId == mainVm.CreatedTask.Task.ProjectId)
                {
                    mainVm.Tasks.Insert(0, mainVm.CreatedTask);
                }
            };
            mainVm.CreatedTask.OnRemove = x => this.Tasks.Remove(x);
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
