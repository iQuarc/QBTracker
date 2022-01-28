using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Humanizer;
using Humanizer.Localisation;
using QBTracker.DataAccess;

namespace QBTracker.Converters
{
    internal class MonthTimeConverter : IMultiValueConverter
    {
        public MonthTimeConverter()
        {
            this.Repository = (IRepository)Application.Current.Resources["Repository"];
        }

        private IRepository Repository { get; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime)values[0];
            var timeAggregate = Repository.GetDayAggregatedMonthTime(date);
            return timeAggregate.Humanize(2, maxUnit:TimeUnit.Hour, minUnit: TimeUnit.Minute);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
