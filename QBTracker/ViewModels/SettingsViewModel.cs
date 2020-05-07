using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using QBTracker.AutomaticUpdader;
using QBTracker.Model;
using QBTracker.Util;
using Task = System.Threading.Tasks.Task;

namespace QBTracker.ViewModels
{
    public class SettingsViewModel: ValidatableModel
    {
        private readonly MainWindowViewModel mainVm;
        private float downloadingProgress;
        private bool isDownloading;
        private bool updateAvailable;
        private string downloadMessage = "Update";
        private DispatcherTimer updateTimer;
        public UpdaterService UpdaterService { get; }
        public SettingsViewModel(MainWindowViewModel mainVm)
        {
            this.mainVm = mainVm;
            this.Settings = mainVm.Repository.GetSettings();
            BundledTheme = Application.Current.Resources.MergedDictionaries.OfType<BundledTheme>().First();
            UpdaterService = new UpdaterService(this.mainVm.Repository.GetLiteRepository());
            GoBack = new RelayCommand(_ => mainVm.GoBack());
            DownloadUpdate = new RelayCommand(ExecuteDownloadUpdate);
        }

        public Settings Settings { get; }

        private async void ExecuteDownloadUpdate(object o)
        {
            IsDownloading = true;
            DownloadMessage = "0%";
            if (await UpdaterService.DownloadUpdate(p =>
            {
                this.DownloadingProgress = p;
                this.DownloadMessage = p.ToString("P");
            }))
            {
                UpdaterService.StartUpdater();
                Application.Current.Shutdown();
            }
            else
            {
                IsDownloading = false;
                this.DownloadMessage = "Update";
            }
        }

        public float DownloadingProgress
        {
            get => downloadingProgress;
            set
            {
                downloadingProgress = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsDownloading
        {
            get => isDownloading;
            set
            {
                isDownloading = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanDownload));
            }
        }

        public string DownloadMessage
        {
            get => downloadMessage;
            set
            {
                downloadMessage = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanDownload => !IsDownloading && UpdateAvailable;

        public bool IsDark
        {
            get => BundledTheme.BaseTheme == BaseTheme.Dark;
            set
            {
                BundledTheme.BaseTheme = value ? BaseTheme.Dark : BaseTheme.Light;
                mainVm.Repository.GetSettings().IsDark = value;
                mainVm.Repository.UpdateSettings();
                NotifyOfPropertyChange();
            }
        }

        public PrimaryColor? PrimaryColor
        {
            get => BundledTheme.PrimaryColor;
            set
            {
                BundledTheme.PrimaryColor = value;
                mainVm.Repository.GetSettings().PrimaryColor = value;
                mainVm.Repository.UpdateSettings();
                NotifyOfPropertyChange();
            }
        }

        public SecondaryColor? SecondaryColor
        {
            get => BundledTheme.SecondaryColor;
            set
            {
                BundledTheme.SecondaryColor = value;
                mainVm.Repository.GetSettings().SecondaryColor = value;
                mainVm.Repository.UpdateSettings();
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<PrimaryColor> PrimaryColors => (PrimaryColor[]) Enum.GetValues(typeof(PrimaryColor));
        public IEnumerable<SecondaryColor> SecondaryColors => (SecondaryColor[])Enum.GetValues(typeof(SecondaryColor));
        public BundledTheme BundledTheme { get; }
        public RelayCommand GoBack { get; }
        public RelayCommand DownloadUpdate { get; }

        public async Task<bool> CheckForUpdateSequence(bool force = false)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            if (await UpdaterService.CheckForUpdate(force))
            {
                this.UpdateAvailable = true;
                return true;
            }
            this.UpdateAvailable = false;
            if (updateTimer == null)
            {
                this.updateTimer = new DispatcherTimer();
                this.updateTimer.Interval = TimeSpan.FromDays(1);
                this.updateTimer.Tick += async (o, e) => await CheckForUpdateSequence(true);
                this.updateTimer.Start();
            }

            return false;
        }

        public bool UpdateAvailable
        {
            get => updateAvailable;
            set
            {
                updateAvailable = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanDownload));
                NotifyOfPropertyChange(nameof(UpdateVersion));
            }
        }

        public bool AutomaticallyStart
        {
            get => Settings.StartWithWindows;
            set
            {
                Settings.StartWithWindows = value;
                RegisterInStartup(value);
                NotifyOfPropertyChange();
            }
        }

        private void RegisterInStartup(bool isChecked)
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            var processModule = Process.GetCurrentProcess().MainModule;
            if (registryKey == null || processModule == null)
                return;
            if (isChecked)
            {
                registryKey.SetValue("QBTracker", processModule.FileName);
            }
            else
            {
                registryKey.DeleteValue("QBTracker", false);
            }
        }

        public string UpdateVersion => UpdaterService?.ReleaseToUpdate?.ParsedVersion?.ToString(3);

        public string Debug
        {
            get
            {
                var s = $@"Exe: {Application.Current.StartupUri}
CodeBase: {Assembly.GetExecutingAssembly().GetName().CodeBase}
BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}
Process: {Process.GetCurrentProcess().MainModule?.FileName}
Environment: {Environment.CurrentDirectory}
";
return s;
            }
        }
    }
}