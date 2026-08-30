#define MyAppName "LANtern"
#define MyAppVersion "0.1.1"
#define MyAppPublisher "Volkan Demir"
#define MyAppURL "https://github.com/VolkanDemir74"
#define StageDir "..\artifacts\installer-stage"

[Setup]
AppId={{7CB12F72-7DAF-49EE-83EE-A159B08EE3F4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\LANtern
DefaultGroupName=LANtern
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=LANtern-Setup-x64
SetupIconFile=..\src\LANtern.Host\Assets\LANtern.ico
UninstallDisplayIcon={app}\LANtern.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "launch"; Description: "Kurulumdan sonra LANtern'ı çalıştır / Launch LANtern after setup"; Flags: checkedonce

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\LANtern"; Filename: "{app}\LANtern.exe"
Name: "{group}\LANtern'ı kaldır"; Filename: "{uninstallexe}"
Name: "{autodesktop}\LANtern"; Filename: "{app}\LANtern.exe"; Tasks: desktopicon

[Run]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM LANtern.exe"; Flags: runhidden waituntilterminated; StatusMsg: "Eski LANtern işlemleri kapatılıyor..."; Check: IsUpgrade
Filename: "{app}\LANtern.DeviceService.exe"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; Check: IsUpgrade
#ifdef DevelopmentDriver
Filename: "{sys}\certutil.exe"; Parameters: "-f -addstore Root ""{app}\driver\LANtern-Test.cer"""; Flags: runhidden waituntilterminated
Filename: "{sys}\certutil.exe"; Parameters: "-f -addstore TrustedPublisher ""{app}\driver\LANtern-Test.cer"""; Flags: runhidden waituntilterminated
#endif
Filename: "{sys}\pnputil.exe"; Parameters: "/add-driver ""{app}\driver\IddSampleDriver.inf"" /install"; Flags: runhidden waituntilterminated; StatusMsg: "LANtern sanal monitör sürücüsü kuruluyor..."
Filename: "{app}\LANtern.DeviceService.exe"; Parameters: "--install"; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""LANtern LAN HTTP"""; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""LANtern LAN HTTP"" dir=in action=allow program=""{app}\LANtern.exe"" protocol=TCP localport=5000 profile=private enable=yes"; Flags: runhidden waituntilterminated
Filename: "{app}\LANtern.exe"; Description: "LANtern'ı çalıştır / Launch LANtern"; Flags: nowait postinstall skipifsilent; Tasks: launch

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM LANtern.exe"; Flags: runhidden waituntilterminated; RunOnceId: "StopHost"
Filename: "{app}\LANtern.DeviceService.exe"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveDeviceService"
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Uninstall-LANternDriver.ps1"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveDriver"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""LANtern LAN HTTP"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveFirewall"

[Code]
function IsUpgrade(): Boolean;
begin
  Result := RegKeyExists(HKLM64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{7CB12F72-7DAF-49EE-83EE-A159B08EE3F4}_is1');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'LANtern');
    if MsgBox('LANtern kullanıcı ayarları da silinsin mi?' + #13#10 + 'Delete LANtern user settings as well?', mbConfirmation, MB_YESNO) = IDYES then
      DelTree(ExpandConstant('{localappdata}\LANtern'), True, True, True);
  end;
end;
