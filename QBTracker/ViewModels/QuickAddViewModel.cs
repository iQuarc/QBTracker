using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;

using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

using QBTracker.DataAccess;
using QBTracker.Model;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class QuickAddViewModel : ValidatableModel
    {
        private const int MinutesPerInterval = 15;
        private int? _selectedProjectId;
        private int? _selectedTaskId;
        private int selectedIntervals;

        public QuickAddViewModel(MainWindowViewModel mainVm)
        {
            MainVm = mainVm;
            this.TimeRecord = new TimeRecord();
            this.SelectedProjectId = mainVm.SelectedProjectId ?? throw new InvalidOperationException($"{nameof(mainVm.SelectedProjectId)} cannot be null");
            this.SelectedTaskId = mainVm.SelectedTaskId ?? throw new InvalidOperationException($"{nameof(mainVm.SelectedTaskId)} cannot be null");
            var maxDate = mainVm.TimeRecords.Aggregate((DateTime?)null, (s, t) => MaxDate(s, t.EndTime));
            this.StartTime = maxDate?.ToLocalTime() ?? mainVm.SelectedDate.Date.ToLocalTime().Add(TimeSpan.FromHours(8));
            this.SelectedIntervals = 4;
        }

        public MainWindowViewModel MainVm { get; }

        public TimeRecord TimeRecord { get; }

        [Required, Display(Name = "Project")]
        public int? SelectedProjectId
        {
            get => _selectedProjectId;
            set
            {
                _selectedProjectId = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanSave));
                LoadTasks();
                if (SelectedProjectId != null)
                {
                    var project = MainVm.Repository.GetProjectById(SelectedProjectId.Value);
                    this.TimeRecord.ProjectId = SelectedProjectId.Value;
                    this.TimeRecord.ProjectName = project.Name;
                }
            }
        }

        [Required, Display(Name = "Task")]
        public int? SelectedTaskId
        {
            get => _selectedTaskId;
            set
            {
                
                _selectedTaskId = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanSave));
                if (SelectedTaskId != null)
                {
                    var task = MainVm.Repository.GetTaskById(SelectedTaskId.Value);
                    this.TimeRecord.TaskId = SelectedTaskId.Value;
                    this.TimeRecord.TaskName = task.Name;
                }
            }
        }

        [Required, Display(Name = "Start time")]
        public DateTime? StartTime
        {
            get => TimeRecord.StartTime == DateTime.MinValue ? null : TimeRecord.StartTime.ToLocalTime();
            set
            {
                if (value == null)
                {
                    TimeRecord.StartTime = DateTime.MinValue;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(CanSave));
                    return;
                }

                DateTime newValue = value.Value.ToUniversalTime();
                if (newValue == value)
                    return;
                TimeRecord.StartTime = value.Value.ToUniversalTime();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        [Required, Display(Name = "End time")]
        public DateTime? EndTime
        {
            get => TimeRecord.EndTime?.ToLocalTime() ?? StartTime;
            set
            {
                if (value == null)
                {
                    TimeRecord.EndTime = null;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(CanSave));
                    return;
                }
                DateTime newValue = value.Value.ToUniversalTime();
                if (newValue == value)
                    return;
                TimeRecord.EndTime = newValue;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
                CalcSeledIntervals();
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public string DurationText => Duration?.ToString(@"h\h\ m\m");
        public TimeSpan? Duration => EndTime - StartTime;


        public int SelectedIntervals
        {
            get => selectedIntervals;
            set
            {
                if (selectedIntervals == value)
                    return;
                selectedIntervals = value;
                NotifyOfPropertyChange();
                CalcEndTime();
            }
        }

        public bool CanSave => 
            this.SelectedProjectId.HasValue && 
            this.SelectedTaskId.HasValue && 
            this.StartTime.HasValue && 
            this.EndTime.HasValue;

        private void CalcSeledIntervals()
        {
            var intervals = Math.Round(((StartTime - EndTime)?.TotalMinutes ?? 0) / MinutesPerInterval);
            SelectedIntervals = (int)Math.Abs(intervals);
        }

        private void CalcEndTime()
        {
            var targetEndTime = this.StartTime?.AddMinutes(MinutesPerInterval * SelectedIntervals);
            if (Math.Abs((targetEndTime - EndTime)?.TotalMinutes ?? 0) >= MinutesPerInterval)
            {
                this.EndTime = targetEndTime;
            }
        }

        public ObservableRangeCollection<TaskViewModel> Tasks { get; } = new ObservableRangeCollection<TaskViewModel>();

        private void LoadTasks()
        {
            Tasks.Clear();
            if (SelectedProjectId.HasValue)
            {
                Tasks.AddRange(MainVm.Repository.GetTasks(SelectedProjectId.Value)
                        .Select(x => new TaskViewModel(x, this.MainVm)));
            }
        }

        private DateTime? MaxDate(DateTime? first, DateTime? second)
        {
            if (first == null && second == null)
                return null;
            if (first == null)
                return second;
            if (second == null)
                return first;
            return first >= second ? first : second;
        }
    }
}
