﻿using QBTracker.ViewModels;

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
            this.ShowInTaskbar = true;
            this.Activate();
            this.Focus();
        }

        private void HideWindow()
        {
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }
    }
}
