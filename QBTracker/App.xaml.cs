using System.Linq;
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
        protected override void OnStartup(StartupEventArgs e)
        {
            var repository = new Repository();
            var settings = repository.GetSettings();
            if (settings.IsDark.HasValue && settings.PrimaryColor.HasValue && settings.SecondaryColor.HasValue)
            {
                var bundle =  this.Resources.MergedDictionaries.OfType<BundledTheme>().First();
                bundle.BaseTheme = settings.IsDark == true ? BaseTheme.Dark : BaseTheme.Light;
                bundle.PrimaryColor = settings.PrimaryColor.Value;
                bundle.SecondaryColor = settings.SecondaryColor.Value;
            }
            base.OnStartup(e);
        }
    }
}
