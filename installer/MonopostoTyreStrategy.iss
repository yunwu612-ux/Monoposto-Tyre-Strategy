#define MyAppName "Monoposto Tyre Strategy"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "YunWu"
#define MyAppExeName "MonopostoTyreStrategy.exe"

[Setup]
AppId={{D57E8C15-57B0-4D26-AE16-8C7AA84D9A21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Monoposto Tyre Strategy
DefaultGroupName={#MyAppName}
OutputDir=..\output
OutputBaseFilename=Monoposto-Tyre-Strategy-V1.1-Setup-Windows-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "{#GetEnv('APP_EXE')}"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: postinstall nowait skipifsilent
