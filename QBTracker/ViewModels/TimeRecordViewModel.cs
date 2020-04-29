using Humanizer;

using MaterialDesignThemes.Wpf;

using QBTracker.Model;
using QBTracker.Util;
using QBTracker.Views;

using System;
using System.Windows.Threading;

namespace QBTracker.ViewModels
{
    public class TimeRecordViewModel : ValidatableModel
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private DispatcherTimer timer;

        public TimeRecordViewModel(TimeRecord timeRecord, MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            TimeRecord = timeRecord;
            DeleteCommand = new RelayCommand(ExecuteDelete);
            if (!EndTime.HasValue)
            {
                timer = new DispatcherTimer();
                timer.Tick += TimerOnTick;
                timer.Interval += TimeSpan.FromSeconds(1);
                timer.Start();
            }
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            NotifyOfPropertyChange(nameof(Duration));
        }

        public TimeRecord TimeRecord { get; }

        public DateTime StarTime
        {
            get => TimeRecord.StartTime.ToLocalTime();
            set
            {
                TimeRecord.StartTime = value.ToUniversalTime();
                NotifyOfPropertyChange();
            }
        }

        public DateTime? EndTime
        {
            get => TimeRecord.EndTime;
            set
            {
                if (value != null)
                {
                    TimeRecord.EndTime = value.Value.ToUniversalTime();
                    timer.Stop();
                }
                else
                    TimeRecord.EndTime = null;
                NotifyOfPropertyChange();
            }
        }

        public string Duration => EndTime.HasValue 
            ? (EndTime.Value - StarTime).ToString(@"hh\:mm\:ss") 
            : (DateTime.Now - StarTime).ToString(@"hh\:mm\:ss");


        public RelayCommand DeleteCommand { get; }
        private async void ExecuteDelete(object o)
        {
            if ((bool)await DialogHost.Show(new ConfirmDialog
            {
                DataContext = "Are you sure?"
            }))
            {
                this._mainWindowViewModel.Repository.DeleteTimeRecord(this.TimeRecord.Id);
                this._mainWindowViewModel.TimeRecords.Remove(this);
            }
        }
    }
}
