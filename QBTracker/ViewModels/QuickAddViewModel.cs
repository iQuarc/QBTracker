using System;
using System.Collections.Generic;
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
        private int _selectedProjectId;
        private int _selectedTaskId;

        public QuickAddViewModel(MainWindowViewModel mainVm)
        {
            MainVm = mainVm;
            this.SelectedProjectId = mainVm.SelectedProjectId ?? throw new InvalidOperationException($"{nameof(mainVm.SelectedProjectId)} cannot be null"); 
            this.SelectedTaskId = mainVm.SelectedTaskId ?? throw new InvalidOperationException($"{nameof(mainVm.SelectedTaskId)} cannot be null");
            this.TimeRecord = new TimeRecord
            {
                ProjectId = SelectedProjectId,
                TaskId = SelectedTaskId
            };
            var maxDate = mainVm.TimeRecords.Aggregate((DateTime?)null, (s, t) => MaxDate(s, t.EndTime));
            this.StartTime = maxDate?.ToLocalTime() ?? mainVm.SelectedDate.Date.ToLocalTime().Add(TimeSpan.FromHours(8));
            this.EndTime = this.StartTime.AddHours(4);
        }

        public MainWindowViewModel MainVm { get; }

        public TimeRecord TimeRecord { get; }

        public int SelectedProjectId
        {
            get => _selectedProjectId;
            set
            {
                _selectedProjectId = value;
                NotifyOfPropertyChange();
                LoadTasks();
            }
        }

        public int SelectedTaskId
        {
            get => _selectedTaskId;
            set
            {
                _selectedTaskId = value;
                NotifyOfPropertyChange();
            }
        }


        public DateTime StartTime
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

        public DateTime EndTime
        {
            get => TimeRecord.EndTime?.ToLocalTime() ?? StartTime;
            set
            {
                TimeRecord.EndTime = value.ToUniversalTime();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(Duration));
                NotifyOfPropertyChange(nameof(DurationText));
            }
        }

        public string DurationText => Duration.ToString(@"h\h\ m\m");
        public TimeSpan Duration => EndTime - StartTime;

        public ObservableRangeCollection<TaskViewModel> Tasks { get; } = new ObservableRangeCollection<TaskViewModel>();

        private void LoadTasks()
        {
            Tasks.Clear();
            Tasks.AddRange(MainVm.Repository.GetTasks(SelectedProjectId)
                    .Select(x => new TaskViewModel(x, this.MainVm)));
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
