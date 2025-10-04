# DesktopRestorer

DesktopRestorer 是一个简单实用的桌面备份和恢复工具，帮助您在系统重启或更改后快速恢复桌面文件和图标布局。

## 功能特点

- 备份桌面文件到安全位置
- 一键恢复桌面文件
- 进度条显示备份/恢复进度
- 系统托盘支持，最小化不打扰
- 简洁直观的用户界面

## 安装说明

### 方法一：直接运行可执行文件

1. 下载 `DesktopRestorer.exe` 文件
2. 双击运行程序
3. 需要 .NET 8.0 Desktop Runtime 支持

### 方法二：使用安装程序

1. 安装脚本位于 `installer/DesktopRestorer.iss`
2. 打开 Inno Setup 编译器加载该脚本并编译
3. 生成的安装包 `DesktopRestorerSetup.exe` 可在编译输出目录中找到
4. 双击运行安装程序，按照向导完成安装
5. 从开始菜单或桌面快捷方式启动

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

## 系统要求

- Windows 10/11
- .NET 8.0 Desktop Runtime

## 注意事项

- 首次使用前建议进行一次桌面备份
- 恢复操作会覆盖当前桌面文件，请确保重要文件已备份
- 程序需要读写桌面文件夹的权限

## 许可证

- 本项目采用 PolyForm Noncommercial License 1.0.0（非商业许可）。
- 允许非商业用途的使用、修改与分发；任何商业用途均不被许可。
- 详细条款见 `LICENSE` 文件或访问 `https://polyformproject.org/licenses/noncommercial/1.0.0/`。
- 如需商业授权，请联系项目作者以获取单独许可。