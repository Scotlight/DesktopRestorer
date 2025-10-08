using System;
using System.Windows;
using System.Windows.Controls;

namespace DesktopRestorer
{
    public partial class MainWindow : Window
    {
        private void SystemShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSystemShortcutsWindow();
        }
        
        private void OpenSystemShortcutsWindow()
        {
            var shortcutsWindow = new SystemShortcutsWindow();
            shortcutsWindow.Owner = this;
            shortcutsWindow.ShowDialog();
        }

        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement folder browser dialog
        }

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Implement auto-start logic
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Implement auto-start logic
        }

        private void AutoBackupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Implement auto-backup logic
        }

        private void AutoBackupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Implement auto-backup logic
        }

        private void BackupNowButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement backup logic
        }

        private void RestoreNowButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement restore logic
        }
    }
}