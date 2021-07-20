using System;
using System.Linq;
using System.Threading;
using System.Windows;
using MaterialDesignThemes.Wpf;
using QBTracker.DataAccess;

namespace QBTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            #if !DEBUG
            const string mutexName = @"Global\QBTracker";

            mutex = new Mutex(true, mutexName, out var createdNew);
            if (!createdNew)
                Environment.Exit(0);

            using (var repository = new Repository())
            {
                var settings = repository.GetSettings();
                if (settings.IsDark.HasValue && settings.PrimaryColor.HasValue && settings.SecondaryColor.HasValue)
                {
                    var bundle = this.Resources.MergedDictionaries.OfType<BundledTheme>().First();
                    bundle.BaseTheme = settings.IsDark == true ? BaseTheme.Dark : BaseTheme.Light;
                    bundle.PrimaryColor = settings.PrimaryColor.Value;
                    bundle.SecondaryColor = settings.SecondaryColor.Value;
                }
            }
            #endif
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex.ReleaseMutex();
        }
    }
}
