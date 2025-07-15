using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using MaterialDesignThemes.Wpf;
using QBTracker.DataAccess;
using QBTracker.Model;
#pragma warning disable CA1416

namespace QBTracker
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
#if !DEBUG
        private Mutex mutex;
#endif

      private string appDataFolder = "App_Data";

      protected override void OnStartup(StartupEventArgs e)
      {
#if !DEBUG
         var appDAta   = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         appDataFolder = Path.Combine(appDAta, "QBTracker");
#endif
#if !DEBUG
            const string mutexName = @"Global\QBTracker";

            mutex = new Mutex(true, mutexName, out var createdNew);
            if (!createdNew)
                Environment.Exit(0);
#endif
         using (var repository = new Repository())
         {
            var settings = repository.GetSettings();
            if (settings is { IsDark: not null, PrimaryColor: not null, SecondaryColor: not null })
            {
               var bundle = this.Resources.MergedDictionaries.OfType<BundledTheme>().First();
               bundle.BaseTheme      = settings.IsDark == true ? BaseTheme.Dark : BaseTheme.Light;
               bundle.PrimaryColor   = settings.PrimaryColor.Value;
               bundle.SecondaryColor = settings.SecondaryColor.Value;
            }
         }

         base.OnStartup(e);

         AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      }

      private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         var err = new StringBuilder()
            .AppendLine()
            .AppendLine($"Unhandled Error: {DateTime.UtcNow}, IsTerminating:{e.IsTerminating}")
            .AppendLine(e.ExceptionObject.ToString());

         File.AppendAllText(Path.Combine(appDataFolder, "CurrentDomain_UnhandledException.log"), err.ToString());

         using var repository = new Repository();
         repository.AddLogEntry(new LogEntry
         {
            Type    = LogEntryType.Error,
            Message = e.ExceptionObject.ToString() ?? "unknown error"
         });
      }

      protected override void OnExit(ExitEventArgs e)
      {
#if !DEBUG
            mutex.ReleaseMutex();
#endif
      }
   }
}