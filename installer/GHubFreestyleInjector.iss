#define AppName "G HUB RGB Freestyle Injector"
#define AppId "GHubFreestyleInjector"
#define AppVersion GetEnv("GHUB_FREESTYLE_VERSION")
#define BinaryVersion "0.1.1.0"
#if AppVersion == ""
  #define AppVersion "0.1.1"
#endif
#define SourceDir GetEnv("GHUB_FREESTYLE_SOURCE_DIR")
#if SourceDir == ""
  #define SourceDir "..\artifacts\GHubFreestyleInjector-WinUI3"
#endif
#define OutputDir GetEnv("GHUB_FREESTYLE_INSTALLER_OUT")
#if OutputDir == ""
  #define OutputDir "..\artifacts\installer"
#endif

[Setup]
AppId={{A2A5BD7B-94D7-4CE0-B9E2-5C63F71141C3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=wvxbs
AppPublisherURL=https://github.com/wvxbs/ghub-rgb-freestyle-injector
AppSupportURL=https://github.com/wvxbs/ghub-rgb-freestyle-injector/issues
AppUpdatesURL=https://github.com/wvxbs/ghub-rgb-freestyle-injector/releases
DefaultDirName={localappdata}\Programs\GHubFreestyleInjector
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir={#OutputDir}
OutputBaseFilename=GHubFreestyleInjector-Setup-windows-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\GHubFreestyleInjector.WinUI.exe
VersionInfoVersion={#BinaryVersion}
VersionInfoCompany=wvxbs
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\GHubFreestyleInjector.WinUI.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\GHubFreestyleInjector.WinUI.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\GHubFreestyleInjector.WinUI.exe"; Description: "Abrir {#AppName}"; Flags: nowait postinstall skipifsilent
