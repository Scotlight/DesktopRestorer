using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopRestorer
{
    public partial class MainWindow : Window
    {
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;

        public MainWindow()
        {
            InitializeComponent();
            _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopRestorer");
            _settingsFilePath = Path.Combine(_settingsDirectory, "settings.txt");
            
            // 设置默认目标文件夹为桌面
            TargetFolderTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            LoadSourceFolderFromSettings();
            
            // 初始化日志
            LogMessage("程序已启动");

            EnsureSourceFolderSelected();
        }
        
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
            TrySelectSourceFolder("选择源文件夹");
        }
        
        private void BrowseTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.Description = "选择目标文件夹";
            dialog.ShowNewFolderButton = true;
            
            WinForms.DialogResult result = dialog.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                TargetFolderTextBox.Text = dialog.SelectedPath;
                LogMessage($"已选择目标文件夹: {dialog.SelectedPath}");
            }
        }
        
        private void BackupNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sourceFolder = SourceFolderTextBox.Text;
                string targetFolder = TargetFolderTextBox.Text;
                
                if (string.IsNullOrEmpty(sourceFolder))
                {
                    System.Windows.MessageBox.Show("请选择源文件夹", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (!Directory.Exists(sourceFolder))
                {
                    System.Windows.MessageBox.Show("源文件夹不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                BackupFiles(sourceFolder, targetFolder);
                LogMessage("备份完成");
                System.Windows.MessageBox.Show("备份完成", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"备份失败: {ex.Message}");
                System.Windows.MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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



        private void RestoreNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sourceFolder = SourceFolderTextBox.Text;
                string targetFolder = TargetFolderTextBox.Text;
                
                if (string.IsNullOrEmpty(sourceFolder) || string.IsNullOrEmpty(targetFolder))
                {
                    System.Windows.MessageBox.Show("请选择源文件夹和目标文件夹", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (!Directory.Exists(sourceFolder))
                {
                    System.Windows.MessageBox.Show("源文件夹不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                
                RestoreFiles(sourceFolder, targetFolder);
                LogMessage("还原完成");
                System.Windows.MessageBox.Show("还原完成", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"还原失败: {ex.Message}");
                System.Windows.MessageBox.Show($"还原失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSourceFolderFromSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string savedPath = File.ReadAllText(_settingsFilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath))
                    {
                        SourceFolderTextBox.Text = savedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"读取配置失败: {ex.Message}");
            }
        }

        private void EnsureSourceFolderSelected()
        {
            if (!string.IsNullOrWhiteSpace(SourceFolderTextBox.Text) && Directory.Exists(SourceFolderTextBox.Text))
            {
                return;
            }

            SourceFolderTextBox.Text = string.Empty;

            while (true)
            {
                if (TrySelectSourceFolder("首次使用请选择源文件夹"))
                {
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    "首次使用必须选择源文件夹，点击\"是\"重新选择，点击\"否\"退出程序。",
                    "提示",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    System.Windows.Application.Current?.Shutdown();
                    return;
                }
            }
        }

        private bool TrySelectSourceFolder(string description)
        {
            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = description,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SourceFolderTextBox.Text = dialog.SelectedPath;
                SaveSourceFolderPath(dialog.SelectedPath);
                LogMessage($"已选择源文件夹: {dialog.SelectedPath}");
                return true;
            }

            return false;
        }

        private void SaveSourceFolderPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                if (!Directory.Exists(_settingsDirectory))
                {
                    Directory.CreateDirectory(_settingsDirectory);
                }

                File.WriteAllText(_settingsFilePath, path);
            }
            catch (Exception ex)
            {
                LogMessage($"保存配置失败: {ex.Message}");
            }
        }
        
        private void BackupFiles(string sourceFolder, string targetFolder)
        {
            // 创建备份目录（如果不存在）
            Directory.CreateDirectory(targetFolder);
            
            // 获取源文件夹中的所有文件
            string[] files = Directory.GetFiles(sourceFolder);
            
            // 复制每个文件到目标文件夹
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetFolder, fileName);
                File.Copy(file, destFile, true);
                LogMessage($"已备份: {fileName}");
            }
            
            // 获取源文件夹中的所有子文件夹
            string[] folders = Directory.GetDirectories(sourceFolder);
            
            // 递归复制每个子文件夹
            foreach (string folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                string destFolder = Path.Combine(targetFolder, folderName);
                BackupFiles(folder, destFolder);
            }
        }
        
        private void RestoreFiles(string sourceFolder, string targetFolder)
        {
            // 创建目标目录（如果不存在）
            Directory.CreateDirectory(targetFolder);
            
            // 获取源文件夹中的所有文件
            string[] files = Directory.GetFiles(sourceFolder);
            
            // 复制每个文件到目标文件夹
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetFolder, fileName);
                File.Copy(file, destFile, true);
                LogMessage($"已恢复: {fileName}");
            }
            
            // 获取源文件夹中的所有子文件夹
            string[] folders = Directory.GetDirectories(sourceFolder);
            
            // 递归复制每个子文件夹
            foreach (string folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                string destFolder = Path.Combine(targetFolder, folderName);
                RestoreFiles(folder, destFolder);
            }
        }
        
        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogTextBox.AppendText($"[{timestamp}] {message}\n");
            LogTextBox.ScrollToEnd();
        }
    }
}