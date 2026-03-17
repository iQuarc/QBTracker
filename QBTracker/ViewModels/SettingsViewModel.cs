using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using QBTracker.AutomaticUpdader;
using QBTracker.DataAccess;
using QBTracker.Model;
using QBTracker.Plugin.Contracts;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
   public class SettingsViewModel : ValidatableModel
   {
      private readonly MainWindowViewModel  mainVm;
      private          DispatcherTimer      updateTimer;
      private          IPluginTaskProvider? selectedPlugin;

      public UpdaterService UpdaterService { get; }
      public PluginService  PluginService  { get; }

      public SettingsViewModel(MainWindowViewModel mainVm)
      {
         this.mainVm     = mainVm;
         this.Settings   = mainVm.Repository.GetSettings();
         BundledTheme    = Application.Current.Resources.MergedDictionaries.OfType<BundledTheme>().First();
         UpdaterService  = new UpdaterService(this.mainVm.Repository.GetLiteRepository());
         PluginService   = (PluginService)Application.Current.Resources["PluginService"];
         GoBack          = new RelayCommand(_ => mainVm.GoBack());
         DownloadUpdate  = new RelayCommand(ExecuteDownloadUpdate);
         ClearAggregates = new RelayCommand(ExecuteClearAggregates);

         this.mainVm.PropertyChanged += (s, e) =>
         {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedProjectId))
            {
               NotifyOfPropertyChange(nameof(HasProjectSelected));
               NotifyOfPropertyChange(nameof(HasNoProjectSelected));
               RestoreSelectedPlugin();
               UpdatePluginConfigView();
            }
         };
      }

      public Settings Settings { get; }

      public IReadOnlyList<IPluginTaskProvider> AvailablePlugins => PluginService.AvailablePlugins;

      public IPluginTaskProvider? SelectedPlugin
      {
         get => selectedPlugin;
         set
         {
            if (selectedPlugin == value) return;
            selectedPlugin              = value;
            Settings.SelectedPluginName = selectedPlugin?.Name;
            mainVm.Repository.UpdateSettings();
            NotifyOfPropertyChange();
            UpdatePluginConfigView();
         }
      }

      public object? PluginConfigView
      {
         get;
         private set
         {
            field = value;
            NotifyOfPropertyChange();
         }
      }

      public bool HasProjectSelected   => mainVm.SelectedProjectId.HasValue;
      public bool HasNoProjectSelected => !mainVm.SelectedProjectId.HasValue;

      private void UpdatePluginConfigView()
      {
         if (selectedPlugin == null || !mainVm.SelectedProjectId.HasValue)
         {
            PluginConfigView = null;
            return;
         }

         var configRepo = PluginService.CreateConfigRepository(
            mainVm.SelectedProjectId.Value, selectedPlugin.Name);
         PluginConfigView = selectedPlugin.GetConfigurationView(configRepo);
      }

      private void RestoreSelectedPlugin()
      {
         if (!string.IsNullOrEmpty(Settings.SelectedPluginName))
         {
            selectedPlugin = AvailablePlugins.FirstOrDefault(p => p.Name == Settings.SelectedPluginName);
            if (selectedPlugin != null)
            {
               NotifyOfPropertyChange(nameof(SelectedPlugin));
               UpdatePluginConfigView();
            }
         }
      }

      private async void ExecuteDownloadUpdate(object o)
      {
         IsDownloading   = true;
         DownloadMessage = "0%";
         if (await UpdaterService.DownloadUpdate(p =>
             {
                this.DownloadingProgress = p;
                this.DownloadMessage     = p.ToString("P");
             }))
         {
            UpdaterService.StartUpdater();
            Application.Current.Shutdown();
         }
         else
         {
            IsDownloading        = false;
            this.DownloadMessage = "Update";
         }
      }

      private void ExecuteClearAggregates(object o)
      {
         mainVm.Repository.ClearAggregates();
      }

      public bool ShowDebugInfo
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
         }
      }

      public float DownloadingProgress
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
         }
      }

      public bool IsDownloading
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanDownload));
         }
      }

      public string DownloadMessage
      {
         get;
         set
         {
            field = value;
            NotifyOfPropertyChange();
         }
      } = "Update";

      public bool CanDownload => !IsDownloading && UpdateAvailable;

      public bool IsDark
      {
         get => BundledTheme.BaseTheme == BaseTheme.Dark;
         set
         {
            BundledTheme.BaseTheme                 = value ? BaseTheme.Dark : BaseTheme.Light;
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
            BundledTheme.PrimaryColor                    = value;
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
            BundledTheme.SecondaryColor                    = value;
            mainVm.Repository.GetSettings().SecondaryColor = value;
            mainVm.Repository.UpdateSettings();
            NotifyOfPropertyChange();
         }
      }

      public IEnumerable<PrimaryColor>   PrimaryColors   => (PrimaryColor[])Enum.GetValues(typeof(PrimaryColor));
      public IEnumerable<SecondaryColor> SecondaryColors => (SecondaryColor[])Enum.GetValues(typeof(SecondaryColor));
      public BundledTheme                BundledTheme    { get; }
      public RelayCommand                GoBack          { get; }
      public RelayCommand                DownloadUpdate  { get; }
      public RelayCommand                ClearAggregates { get; }

      public async Task<bool> CheckForUpdateSequence(bool force = false)
      {
         System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(3));
         if (await UpdaterService.CheckForUpdate(force))
         {
            this.UpdateAvailable = true;
            return true;
         }

         this.UpdateAvailable = false;
         if (updateTimer == null)
         {
            this.updateTimer          =  new DispatcherTimer();
            this.updateTimer.Interval =  TimeSpan.FromDays(1);
            this.updateTimer.Tick     += async (o, e) => await CheckForUpdateSequence(true);
            this.updateTimer.Start();
         }

         return false;
      }

      public bool UpdateAvailable
      {
         get;
         set
         {
            field = value;
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
            mainVm.Repository.UpdateSettings();
            NotifyOfPropertyChange();
         }
      }

      private void RegisterInStartup(bool isChecked)
      {
         if (OperatingSystem.IsWindows())
         {
            var registryKey   = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
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