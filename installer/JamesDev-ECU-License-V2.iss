#define AppName "JamesDev ECU License V2"
#define AppVersion "2.0.0"
[Setup]
AppId={{C07DA0B0-88E9-46D1-BC9F-40D5A51CF921}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=JamesDev
DefaultDirName={localappdata}\Programs\JamesDev ECU License V2
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir=..\dist\installer
OutputBaseFilename=JamesDev-ECU-License-V2-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Client\License.Client.exe
[Files]
Source: "..\dist\admin\*"; DestDir: "{app}\Admin"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\client\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\License Admin"; Filename: "{app}\Admin\License.Admin.exe"; WorkingDir: "{app}\Admin"
Name: "{group}\License Client"; Filename: "{app}\Client\License.Client.exe"; WorkingDir: "{app}\Client"
Name: "{group}\Uninstall"; Filename: "{uninstallexe}"
[Run]
Filename: "{app}\Client\License.Client.exe"; Description: "Open License Client"; Flags: nowait postinstall skipifsilent
