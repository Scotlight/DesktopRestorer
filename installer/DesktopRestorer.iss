[Setup]
AppName=DesktopRestorer
AppVersion=1.0
AppPublisher=scott
AppSupportURL=none
DefaultDirName={autopf}\DesktopRestorer
DefaultGroupName=DesktopRestorer
OutputDir=Output
OutputBaseFilename=DesktopRestorerSetup
Compression=lzma
SolidCompression=yes
SetupIconFile=D:\0010\else\shutdown\p578a-xndte-001.ico

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "D:\0010\else\shutdown\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DesktopRestorer"; Filename: "{app}\DesktopRestorer.exe"
Name: "{autodesktop}\DesktopRestorer"; Filename: "{app}\DesktopRestorer.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\DesktopRestorer.exe"; Description: "{cm:LaunchProgram,DesktopRestorer}"; Flags: nowait postinstall skipifsilent