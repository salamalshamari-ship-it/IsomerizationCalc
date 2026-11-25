; Inno Setup script: builds Setup.exe (install + uninstall) from the "publish" folder.
; Easiest: run build_installer.bat (publishes, then compiles this file). Or run publish.bat, then open this file in Inno Setup and press Compile (Ctrl+F9).

#define AppName "IsomerizationCalc"
#define AppVersion "1.0.0"
#define AppExe "IsomerizationCalc.exe"

[Setup]
AppId={{B7E4A6D2-5C1F-4E7B-9A3D-2F6C8E1D4A90}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=installer_output
OutputBaseFilename=IsomerizationCalc_Setup_{#AppVersion}
Compression=lzma2
SolidCompression=yes
SetupIconFile=IsomerizationCalc.ico
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "publish\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
