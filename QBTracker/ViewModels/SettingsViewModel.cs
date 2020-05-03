using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using QBTracker.Util;

namespace QBTracker.ViewModels
{
    public class SettingsViewModel: ValidatableModel
    {
        private readonly MainWindowViewModel mainVm;
        public SettingsViewModel(MainWindowViewModel mainVm)
        {
            this.mainVm = mainVm;
            BundledTheme =
                Application.Current.Resources.MergedDictionaries.OfType<BundledTheme>().First();
            GoBack = new RelayCommand(_ => mainVm.GoBack());
        }

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
    }

    public class MaterialColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = SwatchHelper.Lookup[(MaterialDesignColor)(int) value];
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}