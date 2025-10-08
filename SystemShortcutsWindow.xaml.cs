using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopRestorer
{
    public partial class SystemShortcutsWindow : Window
    {
        public SystemShortcutsWindow()
        {
            InitializeComponent();
        }

        private void CreateShortcut_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;

            string shortcutName = "";
            string targetPath = "";
            string arguments = "";
            string iconLocation = "";

            if (button.Name == "ThisPCButton")
            {
                shortcutName = "此电脑";
                targetPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            }
            else if (button.Name == "RecycleBinButton")
            {
                shortcutName = "回收站";
                targetPath = "::{645FF040-5081-101B-9F08-00AA002F954E}";
            }
            else if (button.Name == "ControlPanelButton")
            {
                shortcutName = "控制面板";
                targetPath = "::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}";
            }
            else if (button.Name == "NetworkButton")
            {
                shortcutName = "网络";
                targetPath = "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
            }
            else if (button.Name == "UsersButton")
            {
                shortcutName = "用户文件夹";
                targetPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else if (button.Name == "DocumentsButton")
            {
                shortcutName = "文档";
                targetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if (!string.IsNullOrEmpty(shortcutName) && !string.IsNullOrEmpty(targetPath))
            {
                try
                {
                    CreateShortcut(shortcutName, targetPath, arguments, iconLocation);
                    System.Windows.MessageBox.Show($"‘{shortcutName}’ shortcut created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating shortcut: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateShortcut(string shortcutName, string targetPath, string arguments, string iconLocation)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutLocation = Path.Combine(desktopPath, shortcutName + ".lnk");

            string script = $@"
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut(""{shortcutLocation}"")
$sc.TargetPath = ""{targetPath}""
$sc.Save()
";
            
            var plainTextBytes = System.Text.Encoding.Unicode.GetBytes(script);
            string encodedCommand = Convert.ToBase64String(plainTextBytes);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"PowerShell script failed: {stderr}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}