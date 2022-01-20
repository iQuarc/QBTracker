using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using QBTracker.DataAccess;

namespace QBTracker.Converters
{
    internal class DayFillConverter : IMultiValueConverter
    {
        public DayFillConverter()
        {
            this.Repository = (IRepository)Application.Current.Resources["Repository"];
        }

        private IRepository Repository { get; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime)values[0];
            var percent = Math.Min(Repository.GetHours(date), 8d) / 8d;
            Repository.GetHours(date);
            return percent * System.Convert.ToDouble(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
