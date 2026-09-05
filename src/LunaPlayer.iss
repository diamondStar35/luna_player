; Inno Setup script for Luna Player.
;
; Build the player first, then compile this:
;
;     dotnet build src\LunaPlayer.csproj -c Release
;     ISCC src\LunaPlayer.iss
;

#define BuildDir "bin\Release\net10.0-windows10.0.19041.0\win-x64"
#define AppExeName "LunaPlayer.exe"

#if !FileExists(AddBackslash(SourcePath) + BuildDir + "\" + AppExeName)
  #error Build the player in Release first: dotnet build src\LunaPlayer.csproj -c Release
#endif

; Read all installer metadata from the application itself. The numeric file version remains separate because
; Windows requires four numeric components for the installer resource.
#define AppExePath AddBackslash(SourcePath) + BuildDir + "\" + AppExeName
#define AppVersion GetFileProductVersion(AppExePath)
#define AppBinaryVersion GetVersionNumbersString(AppExePath)
#define AppCopyright GetFileCopyright(AppExePath)

; These are the same strings the player uses for itself; see Configuration\AppInfo.cs and
; Media\Associations.cs, which must agree with these or the two registrations will not clean up after each
; other.
#define AppName "Luna Player"
#define AppIdentifier "LunaPlayer"
#define AppPublisher "diamondStar35"
#define AppUrl "https://github.com/diamondStar35/luna_player"
#define AppUserModelId AppPublisher + "." + AppIdentifier
#define ProgId AppIdentifier + ".Media"
#define ContextLabel "Play with Luna Player"
#define ContextVerb "play_with_luna"
#define CapabilitiesKey "Software\" + AppIdentifier + "\Capabilities"

; The file types the player opens, as Media\MediaLibrary.cs lists them. The entries at the end are written
; out by looping over this rather than by hand: there are four registrations per type, and a list kept in
; four places is a list that will disagree with itself.
#dim Extensions[23] { \
  ".3gp", ".aac", ".aiff", ".alac", ".avi", ".flac", ".flv", ".m2ts", \
  ".m4a", ".m4v", ".mkv", ".mov", ".mp3", ".mp4", ".mpeg", ".mpg", \
  ".ogg", ".opus", ".ts", ".wav", ".webm", ".wma", ".wmv" }
#define Index 0
#define Extension ""

[Setup]
AppId={{7B2F1C4E-5A93-4D18-9C6B-0E5A7F3D82A1}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppCopyright={#AppCopyright}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}
VersionInfoVersion={#AppBinaryVersion}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=no
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
; Installs for the one user, into their own folder, so it never asks for administrator rights and every
; registration below can live under HKCU. Overriding is disallowed rather than merely defaulted: a run as
; administrator would write the associations into the wrong user's hive. Empty is how Inno spells "no
; override at all"; naming a value here would let the command line ask for a machine-wide install.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=
; The player is built for x64 only.
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; Refuses to overwrite a copy that is still running, which would otherwise fail file by file and leave a
; half-replaced installation. The name is the one Application\SingleInstanceService.cs holds.
AppMutex=Local\{#AppIdentifier}.MainInstance
OutputDir=..\dist
OutputBaseFilename={#AppIdentifier}Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; The licence the player is published under, shown for the user to accept. The old script this was ported
; from skipped the page altogether.
LicenseFile=..\LICENSE
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "associate"; Description: "Open audio and video files with {#AppName}"; GroupDescription: "File associations:"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Everything the player needs is in the build folder, the licence and the notices among it. The debugging
; symbols and the documentation XML are there too and are no use to somebody installing it; the symbols
; alone are four times the size of the player.
Source: "{#BuildDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; AppUserModelID is what lets Windows put a name to the process. The player tells Windows which ID it answers
; to; the shell turns that ID into "Luna Player" by finding a shortcut carrying the same ID, which is why the
; media overlay can name what is playing rather than showing "Unknown app".
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; AppUserModelID: "{#AppUserModelId}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; AppUserModelID: "{#AppUserModelId}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
; The same registrations the player writes for itself from its settings, so a user who never opens that page
; still finds the player where Windows expects it. All of it hangs off the file association task, which the
; user can clear on the way through.

; The program id: what the file types point at, and where the Open and context menu commands live.
Root: HKCU; Subkey: "Software\Classes\{#ProgId}"; ValueType: string; ValueName: ""; ValueData: "{#AppName} media file"; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\{#ProgId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\{#ProgId}\shell\{#ContextVerb}"; ValueType: string; ValueName: ""; ValueData: "{#ContextLabel}"; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\{#ProgId}\shell\{#ContextVerb}\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: associate

; The application registration, which is what puts the player in Open With and names it there.
Root: HKCU; Subkey: "Software\Classes\Applications\{#AppExeName}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#AppName}"; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\Applications\{#AppExeName}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\Applications\{#AppExeName}\shell\{#ContextVerb}"; ValueType: string; ValueName: ""; ValueData: "{#ContextLabel}"; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\Applications\{#AppExeName}\shell\{#ContextVerb}\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: associate

; The Default Programs entry, which is what the Settings app reads when it offers the player as a default.
Root: HKCU; Subkey: "{#CapabilitiesKey}"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#AppName}"; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "{#CapabilitiesKey}"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Play audio and media files with {#AppName}."; Flags: uninsdeletekey; Tasks: associate
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#AppName}"; ValueData: "{#CapabilitiesKey}"; Flags: uninsdeletevalue; Tasks: associate

; One block per file type: Open With support, the default handler, the Open With list, and the entry the
; Settings app reads. Uninstalling takes the default handler back out, which the script this was ported from
; did not - it left every type pointing at a program id that no longer existed. The Open With entry is an
; empty binary value named for the program id, which is the convention that list follows and what
; Media\Associations.cs writes; the old script asked for a value type of none, which makes Inno create the
; key and ignore the name, so the entry was never written at all.
#sub EmitExtension
  #define Extension Extensions[Index]
Root: HKCU; Subkey: "Software\Classes\Applications\{#AppExeName}\SupportedTypes"; ValueType: string; ValueName: "{#Extension}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\{#Extension}"; ValueType: string; ValueName: ""; ValueData: "{#ProgId}"; Flags: uninsdeletevalue; Tasks: associate
Root: HKCU; Subkey: "Software\Classes\{#Extension}\OpenWithProgids"; ValueType: binary; ValueName: "{#ProgId}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate
Root: HKCU; Subkey: "{#CapabilitiesKey}\FileAssociations"; ValueType: string; ValueName: "{#Extension}"; ValueData: "{#ProgId}"; Flags: uninsdeletevalue; Tasks: associate
#endsub
#for {Index = 0; Index < DimOf(Extensions); Index++} EmitExtension
