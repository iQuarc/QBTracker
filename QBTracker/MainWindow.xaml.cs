using QBTracker.ViewModels;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace QBTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;
            DataContext = new MainWindowViewModel();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            var vm = this.DataContext as MainWindowViewModel;
            vm.Repository.Dispose();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
        }

        private void RestoreWindowCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Focus();
        }

        private void CloseWindowCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainWindow_OnLocationChanged(object? sender, EventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            var snapMargin = 25;
            var newLeft = desktopWorkingArea.Right - Width;
            var newTop = desktopWorkingArea.Bottom - Height;
            if (Math.Abs(Left - newLeft) <= snapMargin) Left = newLeft;
            if (Math.Abs(Top - newTop) <= snapMargin) Top = newTop;
        }
    }
}
