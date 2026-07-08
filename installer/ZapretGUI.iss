; Inno Setup script for Zapret GUI
; Сборка: installer\build-installer.ps1 (сам публикует и вызывает ISCC)

#define MyAppName "Zapret GUI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Zapret GUI"
#define MyAppExeName "ZapretGUI.exe"

[Setup]
; Уникальный ID приложения (не менять между версиями — иначе обновление поставится рядом).
AppId={{8F3A2B10-9C4D-4E7A-B1F2-7D6E5C4A3B21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ZapretGUI
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=ZapretGUI-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Приложение работает с драйвером winws — ставим и запускаем с правами администратора.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=..\Assets\ZapretGUI.ico

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Весь выхлоп self-contained publish. appsettings.json и логи НЕ включаем —
; это локальная runtime-конфигурация (создаётся при первом запуске).
Source: "publish\*"; DestDir: "{app}"; Excludes: "appsettings.json,*.log"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
