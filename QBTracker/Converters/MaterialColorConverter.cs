using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignColors;

namespace QBTracker.Converters
{
    public class MaterialColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var color = SwatchHelper.Lookup[(MaterialDesignColor)(int) value];
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}