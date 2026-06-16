# 패치용 EXE 빌드 스크립트 (설치 패키지 생성 없음)
# 사용법:
#   .\build_patch.ps1              # publish + 패치 폴더 구성
#   .\build_patch.ps1 -SkipPublish # 기존 publish 결과물 재사용
param([switch]$SkipPublish)

$project    = "InterfaceWatchDog\InterfaceWatchDog.csproj"
$publishDir = "InterfaceWatchDog\bin\publish\win-x64"
$patchDir   = "Installer\Patch"

$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version |
           Where-Object { $_ } | Select-Object -First 1

Write-Host ""
Write-Host "=== InterfaceWatchDog 패치 빌드 v$version ===" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipPublish) {
    Write-Host "[1/2] dotnet publish 시작..." -ForegroundColor Yellow
    dotnet publish $project `
        -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishReadyToRun=true `
        -p:EnableCompressionInSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "빌드 실패." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[1/2] publish 건너뜀 (-SkipPublish)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "[2/2] 패치 폴더 구성 중..." -ForegroundColor Yellow

New-Item -ItemType Directory -Force $patchDir | Out-Null
Copy-Item "$publishDir\InterfaceWatchDog.exe" "$patchDir\InterfaceWatchDog.exe" -Force
Copy-Item "Installer\apply_patch.ps1"          "$patchDir\apply_patch.ps1"   -Force

$exeMB = [math]::Round((Get-Item "$patchDir\InterfaceWatchDog.exe").Length / 1MB, 1)

Write-Host ""
Write-Host "=== 패치 파일 준비 완료 ===" -ForegroundColor Green
Write-Host "  위치  : $patchDir\"
Write-Host "  파일  : InterfaceWatchDog.exe ($exeMB MB)  +  apply_patch.ps1"
Write-Host "  버전  : v$version"
Write-Host ""
Write-Host "[ 배포 방법 ]"
Write-Host "  1. 서버에 '$patchDir\' 폴더 전체 복사"
Write-Host "  2. 서버에서 (관리자 PowerShell): .\apply_patch.ps1"
Write-Host ""
