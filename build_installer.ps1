#Requires -Version 5.1
<#
.SYNOPSIS
    InterfaceWatchDog 설치 패키지 빌드 스크립트

.DESCRIPTION
    1. dotnet publish (win-x64 단일 파일 자체 포함)
    2. Inno Setup 컴파일 (setup.iss → InterfaceWatchDog_Setup_vX.X.X.exe)

.PARAMETER Version
    패키지 버전 (기본값: setup.iss에서 자동 감지)

.PARAMETER SkipPublish
    dotnet publish 생략 (이미 publish된 경우)

.EXAMPLE
    .\build_installer.ps1
    .\build_installer.ps1 -SkipPublish
#>
param(
    [string]$Version = "",
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Version을 지정하지 않으면 .csproj에서 자동 감지
if ([string]::IsNullOrWhiteSpace($Version)) {
    $csprojPath = Join-Path $PSScriptRoot "InterfaceWatchDog\InterfaceWatchDog.csproj"
    $xml = [xml](Get-Content $csprojPath -Raw)
    $Version = $xml.Project.PropertyGroup.Version
    if ([string]::IsNullOrWhiteSpace($Version)) {
        Write-Host "[경고] .csproj에 <Version> 태그가 없습니다. 기본값 1.0.0 사용." -ForegroundColor Yellow
        $Version = "1.0.0"
    }
}

$Root       = $PSScriptRoot
$ProjectDir = Join-Path $Root "InterfaceWatchDog"
$InstallerDir = Join-Path $Root "Installer"
$PublishDir = Join-Path $ProjectDir "bin\publish\win-x64"
$OutputDir  = Join-Path $InstallerDir "Output"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  InterfaceWatchDog 설치 패키지 빌드" -ForegroundColor Cyan
Write-Host "  버전: $Version" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

#─────────────────────────────────────────────────────────────────────────────
# STEP 1: dotnet publish
#─────────────────────────────────────────────────────────────────────────────
if (-not $SkipPublish) {
    Write-Host "[1/2] dotnet publish 실행 중..." -ForegroundColor Yellow

    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }

    $publishArgs = @(
        "publish",
        (Join-Path $ProjectDir "InterfaceWatchDog.csproj"),
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:PublishReadyToRun=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "--output", $PublishDir
    )

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[오류] dotnet publish 실패 (종료 코드: $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }

    $exePath = Join-Path $PublishDir "InterfaceWatchDog.exe"
    if (-not (Test-Path $exePath)) {
        Write-Host "[오류] 빌드 결과물이 없습니다: $exePath" -ForegroundColor Red
        exit 1
    }

    $size = [math]::Round((Get-Item $exePath).Length / 1MB, 1)
    Write-Host "  → 빌드 완료: InterfaceWatchDog.exe ($size MB)" -ForegroundColor Green
} else {
    Write-Host "[1/2] dotnet publish 생략 (-SkipPublish)" -ForegroundColor DarkGray
}

#─────────────────────────────────────────────────────────────────────────────
# STEP 2: Inno Setup 컴파일
#─────────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/2] Inno Setup 컴파일 중..." -ForegroundColor Yellow

# Inno Setup 설치 경로 탐색 (winget 설치 기본 경로 포함)
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$cmdIscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($cmdIscc) { $isccCandidates += $cmdIscc.Source }
$isccPaths = @($isccCandidates | Where-Object { $_ -and (Test-Path $_) })

if ($isccPaths.Count -eq 0) {
    Write-Host ""
    Write-Host "[경고] Inno Setup 6이 설치되어 있지 않습니다." -ForegroundColor Yellow
    Write-Host "  다운로드: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  dotnet publish 결과물 위치:" -ForegroundColor Cyan
    Write-Host "  $PublishDir" -ForegroundColor White
    Write-Host ""
    Write-Host "  Inno Setup 설치 후 아래 명령으로 패키지를 생성하세요:" -ForegroundColor Cyan
    Write-Host "  ISCC.exe `"$InstallerDir\setup.iss`"" -ForegroundColor White
    exit 0
}

$iscc = $isccPaths[0]
$issFile = Join-Path $InstallerDir "setup.iss"

if (Test-Path $OutputDir) {
    Remove-Item "$OutputDir\*.exe" -Force
}
New-Item -ItemType Directory -Force $OutputDir | Out-Null

& $iscc $issFile "/DMyAppVersion=$Version"
if ($LASTEXITCODE -ne 0) {
    Write-Host "[오류] Inno Setup 컴파일 실패 (종료 코드: $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

$setupExe = Get-ChildItem $OutputDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($setupExe) {
    $size = [math]::Round($setupExe.Length / 1MB, 1)
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host "  빌드 완료!" -ForegroundColor Green
    Write-Host "  $($setupExe.FullName)" -ForegroundColor White
    Write-Host "  크기: $size MB" -ForegroundColor White
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host ""
}
