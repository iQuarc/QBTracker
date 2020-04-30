using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace QBTracker.Util
{
    public static class FocusExtension
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusExtension),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        private static void OnIsFocusedPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var uie = (UIElement)d;
            if ((bool)e.NewValue)
            {
                uie.Focus(); // Don't care about false values.

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input,
                    new Action(delegate () {
                        uie.Focus();         // Set Logical Focus
                        Keyboard.Focus(uie); // Set Keyboard Focus
                    }));
            }
        }
    }
}
