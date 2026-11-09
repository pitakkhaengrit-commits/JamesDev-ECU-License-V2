#define MyAppVersion "2.0.0"
[Setup]
AppId={{BFB81209-4A5C-41E8-82B4-04D61D5CE38A}
AppName=JamesDev ECU License V2
AppVersion={#MyAppVersion}
AppPublisher=JamesDev
DefaultDirName={localappdata}\Programs\JamesDev ECU License V2
DefaultGroupName=JamesDev ECU License V2
PrivilegesRequired=lowest
OutputDir=..\dist\installer
OutputBaseFilename=JamesDev-ECU-License-V2-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible

[Files]
Source: "..\dist\admin\License.Admin.exe"; DestDir: "{app}\admin"; Flags: ignoreversion
Source: "..\dist\client\License.Client.exe"; DestDir: "{app}\client"; Flags: ignoreversion

[Icons]
Name: "{group}\License Admin"; Filename: "{app}\admin\License.Admin.exe"; WorkingDir: "{app}\admin"
Name: "{group}\License Client"; Filename: "{app}\client\License.Client.exe"; WorkingDir: "{app}\client"
Name: "{autodesktop}\JamesDev License Client"; Filename: "{app}\client\License.Client.exe"; WorkingDir: "{app}\client"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut for License Client"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\client\License.Client.exe"; Description: "Launch License Client"; Flags: nowait postinstall skipifsilent
