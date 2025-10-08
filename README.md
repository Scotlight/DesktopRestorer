# DesktopRestorer

DesktopRestorer 是一个简单实用的桌面备份和恢复工具，帮助您在系统重启或更改后快速恢复桌面文件和图标布局。

## 界面预览

| 中文 | English |
| ----- | ------- |
| 下图展示了 DesktopRestorer 的主界面。您可以在其中设置源文件夹（文件夹 A）、目标文件夹（桌面）、启动选项以及自动备份间隔等。下方的操作按钮用于立即备份与恢复桌面，底部的日志框会实时显示应用操作记录。 | The following screenshot shows the main UI of DesktopRestorer. You can configure the source folder (Folder A), target folder (Desktop), startup options, and automatic backup interval. The buttons below trigger immediate backup or desktop restore, and the log box at the bottom displays real-time operation logs. |


![DesktopRestorer UI](docs/2025-10-08_18-00-15.png)

## 功能特点

- 备份桌面文件到安全位置
- 一键恢复桌面文件
- 进度条显示备份/恢复进度
- 系统托盘支持，最小化不打扰
- 简洁直观的用户界面

## 版本与下载

- 最新发布（含 ZIP 与安装包）：https://github.com/Scotlight/DesktopRestorer/releases/tag/v1.1.0
- 构建区别：
  - Windows 7/8 兼容版（ZIP，net472）：解压后运行 `DesktopRestorer.exe`。
  - Windows 10/11 安装版（setup.exe，net8.0）：使用安装程序安装，开始菜单/桌面快捷方式启动。


## 安装说明

### 方法一：Windows 7/8（ZIP，net472）

1. 前往发布页下载 `DesktopRestorer_win7.zip`
2. 解压后双击运行 `DesktopRestorer.exe`
3. 如未安装 .NET Framework 4.7.2，请先安装（微软官方提供下载）

### 方法二：Windows 10/11（安装版，net8.0）

1. 前往发布页下载并运行 `DesktopRestorerSetup.exe`
2. 按照向导完成安装
3. 从开始菜单或桌面快捷方式启动
4. 开发者构建说明：安装脚本位于 `installer/DesktopRestorer.iss`，可使用 Inno Setup 编译器重新生成安装包

## 使用方法

### 备份桌面

1. 启动 DesktopRestorer
2. 点击 "备份桌面" 按钮
3. 等待备份完成，状态栏会显示进度

### 恢复桌面

1. 启动 DesktopRestorer
2. 点击 "恢复桌面" 按钮
3. 等待恢复完成，状态栏会显示进度

### 系统托盘功能

- 点击最小化按钮将程序隐藏到系统托盘
- 双击托盘图标恢复窗口
- 右键托盘图标可选择打开程序或退出

### 开机自启动

- 勾选"开机时自动启动并恢复桌面"选项，程序将在系统启动时自动运行
- 自启动时，程序会自动执行桌面恢复操作，无需手动点击
- 确保已设置好源文件夹，否则自动恢复将无法执行



## 系统要求

- Windows 7/8：需要 .NET Framework 4.7.2
- Windows 10/11：无需安装 .NET 运行时（安装版为自包含构建）

## 注意事项

- 首次使用前建议进行一次桌面备份
- 恢复操作会覆盖当前桌面文件，请确保重要文件已备份
- 程序需要读写桌面文件夹的权限

## 许可证

- 本项目采用 PolyForm Noncommercial License 1.0.0（非商业许可）。
- 允许非商业用途的使用、修改与分发；任何商业用途均不被许可。
- 详细条款见 `LICENSE` 文件或访问 `https://polyformproject.org/licenses/noncommercial/1.0.0/`。
- 如需商业授权，请联系项目作者以获取单独许可。