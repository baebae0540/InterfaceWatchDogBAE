;=============================================================================
; InterfaceWatchDog Setup Script
; 인터페이스 감시 시스템 설치 스크립트
; Inno Setup 6 필요 (https://jrsoftware.org/isinfo.php)
;=============================================================================

#define MyAppName       "InterfaceWatchDog"
#define MyAppNameKor    "인터페이스 감시 시스템"
#ifndef MyAppVersion
  #define MyAppVersion  "1.3.0"
#endif
#define MyAppPublisher  "InterfaceWatchDog"
#define MyAppExeName    "InterfaceWatchDog.exe"
#define MyAppSvcName    "InterfaceWatchDog"
#define PublishDir      "..\InterfaceWatchDog\bin\publish\win-x64"

;=============================================================================
[Setup]
;-- 앱 식별자 (고유 GUID - 변경 금지)
AppId={{A3F7C2B1-4E8D-4A9F-B6C3-2D1E5F8A7B90}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppComments={#MyAppNameKor} - ERWEKA Export Manager / TabmachineIF 감시

;-- 설치 경로
DefaultDirName={autopf64}\{#MyAppName}
DefaultGroupName={#MyAppPublisher}\{#MyAppName}
DisableProgramGroupPage=yes

;-- 출력 설정
OutputDir=Output
OutputBaseFilename=InterfaceWatchDog_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes

;-- 권한: 서비스 등록에 관리자 필요
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

;-- 64비트 전용
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

;-- UI 스타일
WizardStyle=modern
; SetupIconFile=Assets\InterfaceWatchDog.ico  ; TODO: .ico 파일 추가 후 활성화

;-- 재시작
RestartIfNeededByRun=no
CloseApplications=yes
CloseApplicationsFilter=*{#MyAppExeName}

;-- 언인스톨 설정
Uninstallable=yes
UninstallDisplayName={#MyAppName} {#MyAppVersion}
CreateUninstallRegKey=yes

;=============================================================================
[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

;=============================================================================
[Messages]
; 한국어 커스텀 메시지
BeveledLabel=InterfaceWatchDog

;=============================================================================
[CustomMessages]
korean.InstallService=Windows 서비스로 자동 시작 등록 (권장)
korean.ServiceInstalling=Windows 서비스 등록 중...
korean.ServiceUninstalling=Windows 서비스 해제 중...
korean.LaunchAfterInstall=설치 완료 후 프로그램 실행
korean.OpenLogFolder=로그 폴더 열기

;=============================================================================
[Types]
Name: "full";    Description: "전체 설치 (Windows 서비스 포함)"
Name: "apponly"; Description: "앱만 설치 (수동 실행)"
Name: "custom";  Description: "사용자 정의 설치"; Flags: iscustom

;=============================================================================
[Components]
Name: "main";    Description: "InterfaceWatchDog 본체"; Types: full apponly custom; Flags: fixed
Name: "service"; Description: "Windows 서비스 자동 등록";         Types: full

;=============================================================================
[Files]
; 메인 실행 파일 (단일 파일 배포)
Source: "{#PublishDir}\{#MyAppExeName}"; \
    DestDir: "{app}"; \
    Flags: ignoreversion; \
    Components: main

; 데이터 폴더 생성용 더미 (실제 데이터는 ProgramData에 저장됨)
Source: "Assets\README.txt"; \
    DestDir: "{app}"; \
    Flags: ignoreversion; \
    Components: main

;=============================================================================
[Dirs]
; ProgramData 하위 폴더 미리 생성
Name: "{commonappdata}\InterfaceWatchDog";       Permissions: users-modify
Name: "{commonappdata}\InterfaceWatchDog\Logs";  Permissions: users-modify

;=============================================================================
[Icons]
; 시작 메뉴
Name: "{group}\{#MyAppName}";     Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppNameKor}"
Name: "{group}\로그 폴더";         Filename: "{commonappdata}\InterfaceWatchDog\Logs"
Name: "{group}\{#MyAppName} 제거"; Filename: "{uninstallexe}"

; 시작 프로그램 (서비스 미사용 시 트레이 자동 시작)
Name: "{commonstartup}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; \
    Components: not service; \
    Comment: "시스템 시작 시 InterfaceWatchDog 실행"

;=============================================================================
[Registry]
; 앱 경로 등록
Root: HKLM64; Subkey: "SOFTWARE\{#MyAppPublisher}\{#MyAppName}"; \
    ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; \
    Flags: uninsdeletekey

Root: HKLM64; Subkey: "SOFTWARE\{#MyAppPublisher}\{#MyAppName}"; \
    ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

Root: HKLM64; Subkey: "SOFTWARE\{#MyAppPublisher}\{#MyAppName}"; \
    ValueType: string; ValueName: "ConfigPath"; \
    ValueData: "{commonappdata}\InterfaceWatchDog\config.json"

;=============================================================================
[Run]
; 서비스 컴포넌트 선택 시 --install 실행 (설치 프로그램이 관리자로 실행되므로 UAC 없음)
Filename: "{app}\{#MyAppExeName}"; \
    Parameters: "--install"; \
    StatusMsg: "{cm:ServiceInstalling}"; \
    Flags: runhidden waituntilterminated; \
    Components: service

; 설치 완료 후 앱 실행 (선택) — 서비스 미사용 시 트레이 앱으로 바로 시작
Filename: "{app}\{#MyAppExeName}"; \
    Description: "{cm:LaunchAfterInstall}"; \
    Flags: nowait postinstall skipifsilent unchecked

;=============================================================================
[UninstallRun]
; 서비스 중지 및 해제
Filename: "{app}\{#MyAppExeName}"; \
    Parameters: "--uninstall"; \
    StatusMsg: "{cm:ServiceUninstalling}"; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "UninstallService"

;=============================================================================
[UninstallDelete]
; 앱 생성 데이터는 유지 (로그, 설정), 실행 파일만 삭제
; 완전 삭제를 원하면 아래 주석 해제:
; Type: filesandordirs; Name: "{commonappdata}\InterfaceWatchDog"

;=============================================================================
[Code]
//=============================================================================
// 설치 전 실행 중인 앱/서비스 종료
//=============================================================================
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // 이미 실행 중인 트레이 앱 종료
  Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // 서비스 중지 (이미 설치된 경우)
  Exec('sc.exe', 'stop {#MyAppSvcName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

//=============================================================================
// 언인스톨 전 확인
//=============================================================================
function InitializeUninstall(): Boolean;
var
  NL: String;
begin
  NL := Chr(13) + Chr(10);
  Result := MsgBox(
    '{#MyAppName}을(를) 제거하시겠습니까?' + NL + NL +
    '  설정 파일과 로그는 삭제되지 않습니다.' + NL +
    '  (위치: %ProgramData%\InterfaceWatchDog)',
    mbConfirmation, MB_YESNO) = IDYES;
end;

//=============================================================================
// 설치 완료 페이지 커스텀 메시지
//=============================================================================
procedure CurStepChanged(CurStep: TSetupStep);
var
  NL: String;
  Msg: String;
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsComponentSelected('service') then
    begin
      NL := Chr(13) + Chr(10);
      Msg :=
        '설치가 완료되었습니다.' + NL + NL +
        '[Windows 서비스 등록]' + NL +
        '  서비스 등록을 시도했습니다. 서비스 관리자에서 확인하세요.' + NL +
        '  (services.msc → InterfaceWatchDog)' + NL + NL +
        '[최초 설정 필요]' + NL +
        '  프로그램을 실행하여 프로세스명, 실행 경로,' + NL +
        '  PDF 폴더 경로를 설정해 주세요.' + NL + NL +
        '[설정 파일 위치]' + NL +
        '  %ProgramData%\InterfaceWatchDog\config.json' + NL + NL +
        '[로그 파일 위치]' + NL +
        '  %ProgramData%\InterfaceWatchDog\Logs\';
      MsgBox(Msg, mbInformation, MB_OK);
    end;
  end;
end;
