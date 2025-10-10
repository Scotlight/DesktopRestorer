## DesktopRestorer v1.1.1

变更摘要

- 新增/调整
  - 仅打包并提供 net8.0（Windows 10/11）版本安装包。
  - 保留 Windows 7 兼容包（.NET Framework 4.7.2 构建）。
  - 优化打包脚本与发布流程。

已知问题

- 暂无阻断性问题反馈。

使用说明差异

- Windows 10/11（推荐）
  - 使用 `DesktopRestorerSetup.exe`（net8.0）安装。
  - 首次运行如被 SmartScreen 拦截，请选择“仍要运行”。
  - 支持高 DPI 与现代主题。

- Windows 7（兼容）
  - 使用 `DesktopRestorer_win7_x64.zip`（或 `DesktopRestorer_win7.zip`）解压后运行。
  - 需已安装 .NET Framework 4.7.2（或更高 4.x 版本）。
  - UI 与渲染效果与 Win10/11 略有差异，个别 API 功能会退化处理。

文件一览

- `DesktopRestorerSetup.exe`：适用于 Windows 10/11 的安装包（net8.0）。
- `DesktopRestorer_win7_x64.zip`：适用于 Windows 7 x64 的压缩包（net472）。
- `DesktopRestorer_win7.zip`：适用于 Windows 7 的备用压缩包（net472）。

升级建议

- Windows 10/11 用户：直接运行新版安装包覆盖安装即可。
- Windows 7 用户：退出旧版后解压覆盖，或在新目录解压运行。

鸣谢

- 感谢所有提交 Issue 与反馈的用户。


