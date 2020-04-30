using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MaterialDesignThemes.Wpf;

namespace QBTracker.Conrol
{
    /// <summary>
    /// Interaction logic for DateTimePicker.xaml
    /// </summary>
    public partial class DateTimePicker : UserControl
    {
        public DateTimePicker()
        {
            InitializeComponent();
        }

        public void CombinedDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        {
            CombinedCalendar.SelectedDate = DateValue != null ? DateValue.Value.Date : (DateTime?)null;
            CombinedClock.Time = DateValue ?? DateTime.MinValue;
        }

        public void CombinedDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (Equals(eventArgs.Parameter, "1"))
            {
                var combined = CombinedCalendar.SelectedDate.Value.Date + CombinedClock.Time.TimeOfDay;
                this.SetCurrentValue(DateValueProperty, combined);
            }
        }

        public DateTime? DateValue
        {
            get => (DateTime?)GetValue(DateValueProperty);
            set => SetValue(DateValueProperty, value);
        }

        public static readonly DependencyProperty DateValueProperty =
            DependencyProperty.Register("DateValue", typeof(DateTime?), typeof(DateTimePicker), new FrameworkPropertyMetadata(null, 
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));


    }
}
