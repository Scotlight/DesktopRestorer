using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // FolderBrowserDialog
using System.Drawing;
using System.Windows.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using Path = System.IO.Path;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
// Removed SharpVectors usage for simplicity

namespace DesktopRestorer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string _configFilePath = string.Empty;
    private string _sourceFolder = string.Empty;
    private string _desktopPath = string.Empty;
    private DispatcherTimer? _backupTimer;
    private bool _isBackingUp = false;
    private bool _isFirstRun = true;
    private const string AppName = "DesktopRestorer";
    private const string RunRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
private NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
        // 应用启动日志可通过 UI LogMessage 统一输出
        LogMessage("应用启动");

        InitializeApp();
        InitializeTrayIcon();
    }

    private void InitializeApp()
    {
        try
        {
            // 获取桌面路径
            _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DesktopFolderTextBox.Text = _desktopPath;

            // 配置文件路径
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            _configFilePath = Path.Combine(appDataPath, "config.txt");

            // 加载配置
            LoadConfig();

            // 检查自启动状态
            CheckAutoStartStatus();
            
            // 初始化自动备份定时器
            InitializeBackupTimer();
            
            // 检查是否首次运行
            CheckFirstRun();

            LogMessage("应用程序已启动");
        }
        catch (Exception ex)
        {
            LogMessage($"初始化应用程序时出错：{ex.Message}");
            MessageBox.Show($"初始化应用程序时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 初始化托盘图标
    private void InitializeTrayIcon()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "桌面恢复工具"
            };

            // 右键菜单
            var contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("打开(&O)");
            openItem.Click += (s, e) => ShowFromTray();
            var exitItem = new ToolStripMenuItem("退出(&E)");
            exitItem.Click += (s, e) => Close();
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // 双击恢复
            _notifyIcon.DoubleClick += (s, e) => ShowFromTray();

            // 监听窗口状态变化
            this.StateChanged += MainWindow_StateChanged;
        }
        catch (Exception ex)
        {
            LogMessage($"初始化托盘图标失败：{ex.Message}");
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide(); // 隐藏窗口，仅保留托盘
        }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
    
    private void InitializeBackupTimer()
    {
        _backupTimer = new DispatcherTimer();
        _backupTimer.Tick += BackupTimer_Tick;
        UpdateBackupInterval();
    }
    
    private void UpdateBackupInterval()
    {
        if (_backupTimer == null) return;
        
        // 从下拉框读取选定的备份间隔（分钟），默认 60
        int intervalMinutes = 60;
        if (BackupIntervalComboBox.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Content?.ToString(), out int parsed))
        {
            intervalMinutes = parsed;
        }

        _backupTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);

        // 根据复选框决定是否启动定时器
        if (AutoBackupCheckBox.IsChecked == true)
        {
            _backupTimer.Start();
            LogMessage($"自动备份间隔：{intervalMinutes} 分钟（已启用）");
        }
        else
        {
            _backupTimer.Stop();
            LogMessage($"自动备份间隔：{intervalMinutes} 分钟（未启用）");
        }
    }
    
    private void BackupTimer_Tick(object? sender, EventArgs e)
    {
        // 执行自动备份（异步，带进度）
        BackupDesktopAsync();
    }
    
    private void CheckFirstRun()
    {
        // 如果是首次运行且源文件夹为空，提示一次并尝试打开选择框；不递归
        if (_isFirstRun && string.IsNullOrEmpty(_sourceFolder))
        {
            MessageBox.Show("首次运行：请选择用于还原的文件夹（可稍后在界面选择）。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            BrowseSourceButton_Click(null, null);

            // 记录首次运行已提示过，避免重复弹窗
            _isFirstRun = false;

            // 若用户已选择则更新源路径
            if (!string.IsNullOrEmpty(SourceFolderTextBox.Text))
            {
                _sourceFolder = SourceFolderTextBox.Text;
            }
        }
    }
    
    private async void BackupDesktopAsync()
    {
        if (_isBackingUp) return;

        try
        {
            if (string.IsNullOrEmpty(_sourceFolder))
            {
                MessageBox.Show("请先选择源文件夹！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _isBackingUp = true;
            StatusTextBlock.Text = "正在备份桌面...";
            BackupProgressBar.IsIndeterminate = true;
            BackupNowButton.IsEnabled = false;
            RestoreNowButton.IsEnabled = false;
            BrowseSourceButton.IsEnabled = false;

            // 确保源文件夹存在
            if (!Directory.Exists(_sourceFolder))
            {
                Directory.CreateDirectory(_sourceFolder);
                LogMessage($"已创建源文件夹：{_sourceFolder}");
            }

            // 先清理目标文件夹（可能较慢，先用不确定进度）
            await Task.Run(() => CleanFolder(_sourceFolder));

            // 统计需要备份的总项目数（仅文件，排除系统文件）
            int total = await Task.Run(() => CountItemsForBackup(_desktopPath));
            BackupProgressBar.IsIndeterminate = false;
            BackupProgressBar.Minimum = 0;
            BackupProgressBar.Maximum = total > 0 ? total : 1;
            BackupProgressBar.Value = 0;

            int progressed = 0;
            var progress = new Progress<int>(v => { BackupProgressBar.Value = v; });

            await Task.Run(() => CopyFilesToFolderWithProgress(_desktopPath, _sourceFolder, ref progressed, progress));

            StatusTextBlock.Text = "桌面备份完成";
            LogMessage($"桌面备份完成，共备份了 {progressed} 个项目。");
        }
        catch (Exception ex)
        {
            LogMessage($"备份桌面时出错：{ex.Message}");
            StatusTextBlock.Text = "备份失败";
        }
        finally
        {
            _isBackingUp = false;
            BackupNowButton.IsEnabled = true;
            RestoreNowButton.IsEnabled = true;
            BrowseSourceButton.IsEnabled = true;
        }
    }

    private int CountItemsForBackup(string sourceFolder)
    {
        int count = 0;
        try
        {
            foreach (string file in Directory.GetFiles(sourceFolder))
            {
                if (!IsSystemFile(file)) count++;
            }

            foreach (string dir in Directory.GetDirectories(sourceFolder))
            {
                if (IsSystemFolder(dir)) continue;
                count += CountItemsForBackup(dir);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"统计备份项时出错：{ex.Message}");
        }
        return count;
    }

    private void CopyFilesToFolderWithProgress(string sourceFolder, string targetFolder, ref int progressed, IProgress<int> progress)
    {
        try
        {
            // 复制文件
            foreach (string sourceFile in Directory.GetFiles(sourceFolder))
            {
                if (IsSystemFile(sourceFile)) continue;

                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(targetFolder, fileName);
                File.Copy(sourceFile, destFile, true);

                progressed++;
                progress.Report(progressed);
            }

            // 复制文件夹
            foreach (string sourceDir in Directory.GetDirectories(sourceFolder))
            {
                if (IsSystemFolder(sourceDir)) continue;

                string dirName = new DirectoryInfo(sourceDir).Name;
                string destDir = Path.Combine(targetFolder, dirName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                CopyFilesToFolderWithProgress(sourceDir, destDir, ref progressed, progress);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"复制文件时出错：{ex.Message}");
        }
    }

     private void CleanFolder(string folderPath)
     {
         try
         {
             // 删除文件
             foreach (string file in Directory.GetFiles(folderPath))
             {
                 try
                 {
                     File.Delete(file);
                     LogMessage($"已删除文件：{Path.GetFileName(file)}");
                 }
                 catch (Exception ex)
                 {
                     LogMessage($"删除文件 {Path.GetFileName(file)} 时出错：{ex.Message}");
                 }
             }

             // 删除子文件夹
             foreach (string dir in Directory.GetDirectories(folderPath))
             {
                 try
                 {
                     Directory.Delete(dir, true);
                     LogMessage($"已删除文件夹：{new DirectoryInfo(dir).Name}");
                 }
                 catch (Exception ex)
                 {
                     LogMessage($"删除文件夹 {new DirectoryInfo(dir).Name} 时出错：{ex.Message}");
                 }
             }
         }
         catch (Exception ex)
         {
             LogMessage($"清理文件夹 {folderPath} 时出错：{ex.Message}");
         }
     }

     private int CopyFilesToFolder(string sourceFolder, string targetFolder)
     {
         int count = 0;

         try
         {
             // 复制文件
             foreach (string sourceFile in Directory.GetFiles(sourceFolder))
             {
                 string fileName = Path.GetFileName(sourceFile);
                 string destFile = Path.Combine(targetFolder, fileName);

                 try
                 {
                     // 跳过系统文件
                     if (IsSystemFile(sourceFile)) continue;

                     File.Copy(sourceFile, destFile, true);
                     LogMessage($"已备份文件：{fileName}");
                     count++;
                 }
                 catch (Exception ex)
                 {
                     LogMessage($"备份文件 {fileName} 时出错：{ex.Message}");
                 }
             }

             // 复制文件夹
             foreach (string sourceDir in Directory.GetDirectories(sourceFolder))
             {
                 string dirName = new DirectoryInfo(sourceDir).Name;
                 string destDir = Path.Combine(targetFolder, dirName);

                 try
                 {
                     // 跳过系统文件夹
                     if (IsSystemFolder(sourceDir)) continue;

                     // 创建目标目录
                     if (!Directory.Exists(destDir))
                     {
                         Directory.CreateDirectory(destDir);
                     }

                     // 递归复制子文件夹内容
                     count += CopyFilesToFolder(sourceDir, destDir);
                     LogMessage($"已备份文件夹：{dirName}");
                     count++; // 计算文件夹本身
                 }
                 catch (Exception ex)
                 {
                     LogMessage($"备份文件夹 {dirName} 时出错：{ex.Message}");
                 }
             }
         }
         catch (Exception ex)
         {
             LogMessage($"复制文件时出错：{ex.Message}");
         }

         return count;
     }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                string[] lines = File.ReadAllLines(_configFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("SourceFolder="))
                    {
                        _sourceFolder = line.Substring("SourceFolder=".Length);
                        SourceFolderTextBox.Text = _sourceFolder;
                    }
                    else if (line.StartsWith("AutoBackup="))
                    {
                        if (bool.TryParse(line.Substring("AutoBackup=".Length), out bool autoBackup))
                        {
                            AutoBackupCheckBox.IsChecked = autoBackup;
                        }
                    }
                    else if (line.StartsWith("BackupInterval="))
                    {
                        string interval = line.Substring("BackupInterval=".Length);
                        foreach (ComboBoxItem item in BackupIntervalComboBox.Items)
                        {
                            if (item.Content.ToString() == interval)
                            {
                                BackupIntervalComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"加载配置时出错：{ex.Message}");
        }
    }

    private void SaveConfig()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(_configFilePath))
            {
                writer.WriteLine($"SourceFolder={_sourceFolder}");
                writer.WriteLine($"AutoBackup={AutoBackupCheckBox.IsChecked}");
                
                string interval = "60"; // 默认值
                if (BackupIntervalComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
                {
                    interval = selectedItem.Content.ToString()!;
                }
                writer.WriteLine($"BackupInterval={interval}");
            }
            LogMessage("配置已保存");
        }
        catch (Exception ex)
        {
            LogMessage($"保存配置时出错：{ex.Message}");
        }
    }
    
    // 一键备份按钮点击事件
    private void BackupNowButton_Click(object sender, RoutedEventArgs e)
    {
        BackupDesktopAsync();
    }
    
    // 自动备份复选框事件
    private void AutoBackupCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (_backupTimer != null)
        {
            UpdateBackupInterval();
            _backupTimer.Start();
            LogMessage("自动备份已启用");
            SaveConfig();
        }
    }
    
    private void AutoBackupCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_backupTimer != null)
        {
            _backupTimer.Stop();
            LogMessage("自动备份已禁用");
            SaveConfig();
        }
    }
    
    // 备份间隔下拉框选择变更事件
    private void BackupIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateBackupInterval();
        SaveConfig();
    }

    private void BrowseSourceButton_Click(object? sender, RoutedEventArgs? e)
    {
        var dialog = new FolderBrowserDialog
        {
            Description = "选择源文件夹（文件夹A）",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SourceFolderTextBox.Text = dialog.SelectedPath;
            SaveConfig();
        }
    }

    private void RestoreNowButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RestoreDesktopAsync();
    }

    private async void RestoreDesktop()
    {
        string sourcePath = SourceFolderTextBox.Text;
        
        if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
        {
            LogMessage("错误：源文件夹不存在或未设置");
            return;
        }

        try
        {
            StatusTextBlock.Text = "正在恢复桌面...";
            LogMessage("开始恢复桌面...");
            
            // 禁用按钮，防止重复操作
            RestoreNowButton.IsEnabled = false;
            BrowseSourceButton.IsEnabled = false;
            
            await Task.Run(() => 
            {
                // 清空桌面（保留系统文件）
                CleanDesktop();
                
                // 复制文件夹A的内容到桌面
                CopyFolderContents(sourcePath, _desktopPath);
            });
            
            LogMessage("桌面恢复完成");
            StatusTextBlock.Text = "桌面恢复完成";
        }
        catch (Exception ex)
        {
            LogMessage($"恢复桌面时出错：{ex.Message}");
            StatusTextBlock.Text = "恢复失败";
        }
        finally
        {
            // 恢复按钮状态
            RestoreNowButton.IsEnabled = true;
            BrowseSourceButton.IsEnabled = true;
        }
    }

    // 新的异步桌面恢复方法，带进度
    private async Task RestoreDesktopAsync()
    {
        if (_isBackingUp) return; // 避免备份和还原并行

        string sourcePath = SourceFolderTextBox.Text;
        if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
        {
            LogMessage("错误：源文件夹不存在或未设置");
            return;
        }

        try
        {
            _isBackingUp = true; // 复用同一标志位
            StatusTextBlock.Text = "正在恢复桌面...";
            BackupProgressBar.IsIndeterminate = true;
            RestoreNowButton.IsEnabled = false;
            BackupNowButton.IsEnabled = false;
            BrowseSourceButton.IsEnabled = false;

            await Task.Run(() => CleanDesktop());

            int total = await Task.Run(() => CountItemsForBackup(sourcePath));
            BackupProgressBar.IsIndeterminate = false;
            BackupProgressBar.Minimum = 0;
            BackupProgressBar.Maximum = total > 0 ? total : 1;
            BackupProgressBar.Value = 0;

            int progressed = 0;
            var progress = new Progress<int>(v => { BackupProgressBar.Value = v; });
            await Task.Run(() => CopyFilesToFolderWithProgress(sourcePath, _desktopPath, ref progressed, progress));

            LogMessage($"桌面恢复完成，共恢复 {progressed} 个项目。");
            StatusTextBlock.Text = "桌面恢复完成";
        }
        catch (Exception ex)
        {
            LogMessage($"恢复桌面时出错：{ex.Message}");
            StatusTextBlock.Text = "恢复失败";
        }
        finally
        {
            _isBackingUp = false;
            RestoreNowButton.IsEnabled = true;
            BackupNowButton.IsEnabled = true;
            BrowseSourceButton.IsEnabled = true;
        }
    }

    private void CleanDesktop()
    {
        try
        {
            DirectoryInfo desktopDir = new DirectoryInfo(_desktopPath);
            
            // 删除文件
            foreach (FileInfo file in desktopDir.GetFiles())
            {
                // 跳过系统文件和快捷方式
                if (!IsSystemFile(file.FullName))
                {
                    file.Delete();
                    LogMessage($"已删除文件：{file.Name}");
                }
            }
            
            // 删除文件夹
            foreach (DirectoryInfo dir in desktopDir.GetDirectories())
            {
                // 跳过系统文件夹
                if (!IsSystemFolder(dir.FullName))
                {
                    dir.Delete(true);
                    LogMessage($"已删除文件夹：{dir.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"清理桌面时出错：{ex.Message}");
            throw;
        }
    }

    private bool IsSystemFile(string filePath)
    {
        try
        {
            // 检查系统属性
            if ((File.GetAttributes(filePath) & FileAttributes.System) == FileAttributes.System)
                return true;
                
            string fileName = Path.GetFileName(filePath).ToLower();
            string extension = Path.GetExtension(filePath).ToLower();
            
            // 检查是否为desktop.ini
            if (fileName == "desktop.ini")
                return true;
                
            // 检查是否为快捷方式
            if (extension == ".lnk")
            {
                // 检查特殊系统快捷方式
                string[] systemShortcuts = new string[] 
                {
                    "此电脑", "计算机", "computer", "this pc",
                    "回收站", "recycle bin",
                    "控制面板", "control panel",
                    "网络", "network",
                    "internet explorer", "microsoft edge",
                    "用户", "users"
                };
                
                string shortcutName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                foreach (string sysName in systemShortcuts)
                {
                    if (shortcutName.Contains(sysName))
                        return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            LogMessage($"检查系统文件时出错：{ex.Message}");
            return false; // 出错时保守处理，不删除
        }
    }

    private bool IsSystemFolder(string folderPath)
    {
        try
        {
            // 检查系统属性
            if ((File.GetAttributes(folderPath) & FileAttributes.System) == FileAttributes.System)
                return true;
                
            // 检查特殊系统文件夹
            string folderName = new DirectoryInfo(folderPath).Name.ToLower();
            string[] systemFolders = new string[] 
            {
                "appdata", "program files", "windows", "system32",
                "users", "documents and settings"
            };
            
            foreach (string sysFolder in systemFolders)
            {
                if (folderName.Contains(sysFolder))
                    return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            LogMessage($"检查系统文件夹时出错：{ex.Message}");
            return false; // 出错时保守处理，不删除
        }
    }

    private void CopyFolderContents(string sourceFolder, string targetFolder)
    {
        try
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourceFolder);
            
            // 复制文件
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                string destFile = Path.Combine(targetFolder, file.Name);
                
                // 区分处理快捷方式和普通文件
                string extension = file.Extension.ToLower();
                if (extension == ".lnk")
                {
                    // 对于快捷方式，直接复制
                    file.CopyTo(destFile, true);
                    LogMessage($"已复制快捷方式：{file.Name}");
                }
                else
                {
                    // 对于普通文件，直接复制
                    file.CopyTo(destFile, true);
                    LogMessage($"已复制文件：{file.Name}");
                }
            }
            
            // 复制文件夹
            foreach (DirectoryInfo dir in sourceDir.GetDirectories())
            {
                string destDir = Path.Combine(targetFolder, dir.Name);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                CopyFolderContents(dir.FullName, destDir);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"复制文件时出错：{ex.Message}");
            throw;
        }
    }

    private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        SetAutoStart(true);
    }

    private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        SetAutoStart(false);
    }

    private void SetAutoStart(bool enable)
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
            {
                if (key == null)
                {
                    LogMessage("无法打开注册表项");
                    return;
                }

                if (enable)
                {
                    string? appPath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(appPath))
                    {
                        LogMessage("无法获取应用程序路径");
                        return;
                    }
                    key.SetValue(AppName, $"\"{appPath}\" /autorun");
                    LogMessage("已设置开机自启动");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    LogMessage("已取消开机自启动");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"设置自启动失败：{ex.Message}");
        }
    }

    private void CheckAutoStartStatus()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryPath))
            {
                if (key != null)
                {
                    object? value = key.GetValue(AppName);
                    AutoStartCheckBox.IsChecked = (value != null);
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"检查自启动状态失败：{ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        // 在UI线程上更新日志
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => LogMessage(message));
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LogTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        SaveConfig();
        base.OnClosed(e);
    }
}