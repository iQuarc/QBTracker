using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using Humanizer;

namespace QBTracker.Converters
{
    [ValueConversion(typeof(IEnumerable<Enum>), typeof(IEnumerable<ValueDescription>))]
    public class EnumHumanizerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            if (value is IEnumerable en)
                return en.Cast<object>()
                    .Select(x => new ValueDescription(x, x.ToString().Humanize()));
            return new ValueDescription(value, value.ToString().Humanize());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ValueDescription
    {
        public ValueDescription(object value, string description)
        {
            Value = value;
            Description = description;
        }

        public object Value { get; }
        public string Description { get; }
    }
}