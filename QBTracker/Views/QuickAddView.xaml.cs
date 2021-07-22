using QBTracker.ViewModels;

using System;
using System.Collections.Generic;
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

namespace QBTracker.Views
{
    /// <summary>
    /// Interaction logic for QuickAddView.xaml
    /// </summary>
    public partial class QuickAddView : UserControl
    {
        public QuickAddView()
        {
            InitializeComponent();
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.DataContext is QuickAddViewModel vm)
            {
                e.CanExecute = vm.CanSave;
            }
        }
    }
}
