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

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            var vm = this.DataContext as MainWindowViewModel;
            vm.Repository.Dispose();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            HideWindow();
        }

        private void RestoreWindowCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ShowWindow();
        }

        private void CloseWindowCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItemExit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItemShow_OnClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Focus();
        }

        private void HideWindow()
        {
            this.WindowState = WindowState.Minimized;
        }

        private async void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.F10)
            {
                var vm = this.DataContext as MainWindowViewModel;
                await vm.SettingsViewModel.CheckForUpdateSequence(true);
            }

            if (e.Key == Key.F12 && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var vm = this.DataContext as MainWindowViewModel;
                vm.SettingsViewModel.ShowDebugInfo = !vm.SettingsViewModel.ShowDebugInfo;
            }
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Minimized:
                    this.ShowInTaskbar = false;
                    break;
                case WindowState.Normal:
                case WindowState.Maximized:
                    this.ShowInTaskbar = true;
                    break;
            }
        }
    }
}
