using QBTracker.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

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
