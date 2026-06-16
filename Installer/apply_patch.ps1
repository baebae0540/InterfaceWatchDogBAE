# InterfaceWatchDog patch apply script
# Run as Administrator
# Usage:
#   .\apply_patch.ps1
#   .\apply_patch.ps1 -InstallDir "D:\CustomPath\InterfaceWatchDog"
param([string]$InstallDir = "")

# Check administrator privileges
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal   = New-Object Security.Principal.WindowsPrincipal($currentUser)
$isAdmin     = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] Run PowerShell as Administrator." -ForegroundColor Red
    exit 1
}

$serviceName = "InterfaceWatchDog"
$patchExe    = Join-Path $PSScriptRoot "InterfaceWatchDog.exe"

if (-not (Test-Path $patchExe)) {
    Write-Host "[ERROR] Patch file not found: $patchExe" -ForegroundColor Red
    exit 1
}

# Resolve install directory: parameter -> registry -> default
if (-not $InstallDir) {
    $regPath  = "HKLM:\SOFTWARE\InterfaceWatchDog\InterfaceWatchDog"
    $regValue = Get-ItemProperty $regPath -ErrorAction SilentlyContinue
    if ($regValue) {
        $InstallDir = $regValue.InstallPath
    }
}
if (-not $InstallDir) {
    $InstallDir = "C:\Program Files\InterfaceWatchDog"
}

$targetExe = Join-Path $InstallDir "InterfaceWatchDog.exe"

Write-Host ""
Write-Host "=== InterfaceWatchDog Patch ===" -ForegroundColor Cyan
Write-Host "  Source : $patchExe"
Write-Host "  Target : $targetExe"
Write-Host ""

if (-not (Test-Path $InstallDir)) {
    Write-Host "[ERROR] Install folder not found: $InstallDir" -ForegroundColor Red
    Write-Host "  Use setup.exe for a fresh install."
    exit 1
}

# Check and stop service + kill any running EXE process (tray app)
$svc        = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
$wasRunning = $svc -and ($svc.Status -eq "Running")

if ($wasRunning) {
    Write-Host "[1/3] Stopping service..." -ForegroundColor Yellow
    Stop-Service $serviceName -Force
    Start-Sleep -Seconds 2
} else {
    Write-Host "[1/3] Service not running - skip stop." -ForegroundColor Gray
}

# Kill any remaining EXE processes (e.g. tray app running in user session)
$exeName = [System.IO.Path]::GetFileNameWithoutExtension($targetExe)
$procs   = Get-Process -Name $exeName -ErrorAction SilentlyContinue
if ($procs) {
    Write-Host "      Killing running process: $exeName ($($procs.Count) instance(s))..." -ForegroundColor Yellow
    $procs | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Replace EXE
Write-Host "[2/3] Replacing EXE..." -ForegroundColor Yellow
try {
    Copy-Item $patchExe $targetExe -Force
} catch {
    Write-Host "[ERROR] Failed to replace EXE: $_" -ForegroundColor Red
    if ($wasRunning) {
        Write-Host "Attempting to restart service..."
        Start-Service $serviceName -ErrorAction SilentlyContinue
    }
    exit 1
}

# Restart service
if ($wasRunning) {
    Write-Host "[3/3] Restarting service..." -ForegroundColor Yellow
    Start-Service $serviceName
    Start-Sleep -Seconds 2
    $status = (Get-Service -Name $serviceName).Status
    if ($status -eq "Running") {
        $statusColor = "Green"
    } else {
        $statusColor = "Red"
    }
    Write-Host "  Service status: $status" -ForegroundColor $statusColor
} else {
    Write-Host "[3/3] Skip restart (service was not running before patch)." -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Patch complete ===" -ForegroundColor Green
Write-Host ""
