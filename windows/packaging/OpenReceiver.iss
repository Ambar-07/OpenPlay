; Inno Setup Script for OpenReceiver
; This script produces a portable, zero-dependency standalone installer for Windows 10/11.

[Setup]
AppName=OpenReceiver
AppVersion=0.1.0
Publisher=OpenReceiver Community
DefaultDirName={autopf}\OpenReceiver
DefaultGroupName=OpenReceiver
UninstallDisplayIcon={app}\OpenReceiver.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.\Output
OutputBaseFilename=OpenReceiver_Setup_x64
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
Name: "startup"; Description: "Start OpenReceiver automatically when Windows starts"; GroupDescription: "System Integration:"

[Files]
; The main WinUI 3 executable and Core C++ DLLs
Source: "..\build\windows\app\Release\OpenReceiver.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\native\Release\OpenReceiverCore.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\windows\app\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\OpenReceiver"; Filename: "{app}\OpenReceiver.exe"
Name: "{autodesktop}\OpenReceiver"; Filename: "{app}\OpenReceiver.exe"; Tasks: desktopicon
; Add shortcut to Startup folder if task is selected
Name: "{autostartup}\OpenReceiver"; Filename: "{app}\OpenReceiver.exe"; Parameters: "--minimized"; Tasks: startup

[Run]
Filename: "{app}\OpenReceiver.exe"; Description: "Launch OpenReceiver"; Flags: nowait postinstall skipifsilent

[Registry]
; Optional firewall exception registration (requires admin)
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"; ValueType: string; ValueName: "OpenReceiver"; ValueData: "v2.30|Action=Allow|Active=TRUE|Dir=In|Protocol=6|Profile=Private|App={app}\OpenReceiver.exe|Name=OpenReceiver AirPlay|"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"; ValueType: string; ValueName: "OpenReceiverUDP"; ValueData: "v2.30|Action=Allow|Active=TRUE|Dir=In|Protocol=17|Profile=Private|App={app}\OpenReceiver.exe|Name=OpenReceiver UDP|"; Flags: uninsdeletevalue
