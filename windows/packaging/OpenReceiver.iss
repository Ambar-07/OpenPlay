; Inno Setup Script for OpenReceiver
; This script produces a portable, zero-dependency standalone installer for Windows 10/11.

[Setup]
AppName=OpenReceiver
AppVersion=0.1.0
AppPublisher=OpenReceiver Community
DefaultDirName={autopf}\OpenReceiver
DefaultGroupName=OpenReceiver
UninstallDisplayIcon={app}\OpenReceiverApp.exe
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
Source: "..\OpenReceiverApp\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\OpenReceiverApp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\OpenReceiverCore\x64\Release\OpenReceiverCore.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\OpenReceiverApp\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\OpenReceiver"; Filename: "{app}\OpenReceiverApp.exe"
Name: "{autodesktop}\OpenReceiver"; Filename: "{app}\OpenReceiverApp.exe"; Tasks: desktopicon
; Add shortcut to Startup folder if task is selected
Name: "{autostartup}\OpenReceiver"; Filename: "{app}\OpenReceiverApp.exe"; Parameters: "--minimized"; Tasks: startup

[Run]
Filename: "{app}\OpenReceiverApp.exe"; Description: "Launch OpenReceiver"; Flags: nowait postinstall skipifsilent

[Registry]
; Optional firewall exception registration (requires admin)
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"; ValueType: string; ValueName: "OpenReceiver"; ValueData: "v2.30|Action=Allow|Active=TRUE|Dir=In|Protocol=6|Profile=Private|App={app}\OpenReceiverApp.exe|Name=OpenReceiver AirPlay|"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"; ValueType: string; ValueName: "OpenReceiverUDP"; ValueData: "v2.30|Action=Allow|Active=TRUE|Dir=In|Protocol=17|Profile=Private|App={app}\OpenReceiverApp.exe|Name=OpenReceiver UDP|"; Flags: uninsdeletevalue
